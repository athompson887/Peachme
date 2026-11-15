#!/bin/bash
#
# Compile every assembly the way UNITY compiles it, without opening Unity.
# Trimmed from FlightSim2026's script of the same name.
#
# `dotnet test Tools/SimCoreTests` covers SimCore only. Game (driver, views,
# HUD) and GameEditor have no tests, so a green suite says nothing about them,
# and Tests compiles against Unity's NUnit, not the headless one.
#
# Uses Unity's own Roslyn and reference set (from the .csproj files Unity
# generates - open the project in Unity once first). Runs nothing; only
# answers "will the editor accept this".
set -u

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT" || exit 1

EDITOR=$(ls -d /Applications/Unity/Hub/Editor/*/Unity.app/Contents 2>/dev/null | tail -1)
[ -z "$EDITOR" ] && { echo "No Unity editor found under /Applications/Unity/Hub/Editor."; exit 1; }

CSC=$(find "$EDITOR/Resources/Scripting/DotNetSdk" -name csc.dll 2>/dev/null | head -1)
[ -z "$CSC" ] && { echo "No Roslyn in $EDITOR - cannot compile the way Unity does."; exit 1; }

WORK=$(mktemp -d)
trap 'rm -rf "$WORK"' EXIT
fail=0

# Order matters: everything references the SimCore just built, not
# Library/ScriptAssemblies (whatever the editor last managed).
for proj in SimCore Game Tests GameEditor; do
    [ -f "$proj.csproj" ] || { echo "  $proj.csproj missing - open Unity once to generate it"; fail=1; continue; }

    grep -o 'HintPath>[^<]*' "$proj.csproj" | sed 's/HintPath>//' \
        | grep -v '^[[:space:]]*$' | sort -u > "$WORK/$proj.refs"

    # Manifest engine modules, even if Unity hasn't regenerated the csproj yet.
    if [ "$proj" != "SimCore" ]; then
        mods="$EDITOR/Resources/Scripting/Managed/UnityEngine"
        for m in $(grep -o '"com.unity.modules.[a-z]*"' Packages/manifest.json | sed 's/"com.unity.modules.//; s/"//'); do
            dll=$(ls "$mods" | grep -i "^UnityEngine\.${m}Module\.dll$" | head -1)
            [ -n "$dll" ] && ! grep -q "/$dll$" "$WORK/$proj.refs" && echo "$mods/$dll" >> "$WORK/$proj.refs"
        done
    fi

    # THE FILES ON DISK, not the csproj's list (it goes stale between regenerations).
    case "$proj" in
        SimCore)    dir="Assets/SimCore" ;;
        Game)       dir="Assets/Game" ;;
        Tests)      dir="Assets/Tests" ;;
        GameEditor) dir="Assets/Editor" ;;
    esac
    if [ "$proj" = "GameEditor" ]; then
        find "$dir" -name '*.cs' | sort > "$WORK/$proj.src"
    else
        find $dir -name '*.cs' -not -path '*/Editor/*' | sort > "$WORK/$proj.src"
    fi

    {
        sed 's|^|-r:|' "$WORK/$proj.refs"
        [ "$proj" != "SimCore" ] && echo "-r:$WORK/SimCore.dll"
        [ "$proj" = "GameEditor" ] && echo "-r:$WORK/Game.dll"
        echo "-target:library"
        echo "-langversion:9"
        echo "-define:UNITY_EDITOR;UNITY_STANDALONE_OSX"
        echo "-out:$WORK/$proj.dll"
        cat "$WORK/$proj.src"
    } > "$WORK/$proj.rsp"

    # Any error, AND an output, or it failed: a missing reference prints an
    # error with no file in front and writes no dll.
    out=$(dotnet "$CSC" "@$WORK/$proj.rsp" 2>&1 | grep -E "(^|: )error ")
    [ -z "$out" ] && [ ! -f "$WORK/$proj.dll" ] && out="no $proj.dll was written - the compiler did not finish"
    if [ -n "$out" ]; then
        echo "$proj: FAILED"; echo "$out" | head -20; fail=1
    else
        printf '%-10s ok  (%s files)\n' "$proj" "$(wc -l < "$WORK/$proj.src" | tr -d ' ')"
    fi
done

# As the PHONE builds see it: no UNITY_EDITOR, no UnityEditor references.
for plat in IOS ANDROID; do
    for proj in SimCore Game; do
        [ -f "$WORK/$proj.rsp" ] || continue
        grep -v -E 'UnityEditor|TestRunner|nunit' "$WORK/$proj.refs" > "$WORK/$proj.$plat.refs"
        {
            sed 's|^|-r:|' "$WORK/$proj.$plat.refs"
            [ "$proj" != "SimCore" ] && echo "-r:$WORK/SimCore.$plat.dll"
            echo "-target:library"
            echo "-langversion:9"
            echo "-define:UNITY_$plat"
            echo "-out:$WORK/$proj.$plat.dll"
            cat "$WORK/$proj.src"
        } > "$WORK/$proj.$plat.rsp"
        out=$(dotnet "$CSC" "@$WORK/$proj.$plat.rsp" 2>&1 | grep -E "(^|: )error ")
        [ -z "$out" ] && [ ! -f "$WORK/$proj.$plat.dll" ] && out="no $proj.$plat.dll was written"
        [ -n "$out" ] && { echo "$proj as a $plat player: FAILED"; echo "$out" | head -20; fail=1; }
    done
done
[ $fail -eq 0 ] && printf '%-10s ok  (SimCore and Game as iOS and Android players)\n' "players"

# SimCore stays Unity-free. The asmdef says noEngineReferences, but a grep is cheap.
leak=$(grep -rln --include='*.cs' -E 'using UnityEngine|UnityEngine\.' Assets/SimCore || true)
if [ -n "$leak" ]; then
    echo "simcore: FAILED - UnityEngine in SimCore:"; echo "$leak"; fail=1
else
    printf '%-10s ok  (no UnityEngine in SimCore)\n' "simcore"
fi

# No IMGUI: UI Toolkit only (FlightSim2026 rule - OnGUI draws over everything
# and re-lays itself several times a frame).
imgui=$(grep -rn --include='*.cs' -E 'void OnGUI *\( *\)' Assets/Game Assets/Editor 2>/dev/null || true)
if [ -n "$imgui" ]; then
    echo "imgui: FAILED - an OnGUI; draw it in UI Toolkit"; echo "$imgui"; fail=1
else
    printf '%-10s ok  (no OnGUI)\n' "imgui"
fi

exit $fail
