#!/usr/bin/env bash

set -eu
set -o pipefail

# liberated from https://stackoverflow.com/a/18443300/433393
realpath() {
  OURPWD=$PWD
  cd "$(dirname "$1")"
  LINK=$(readlink "$(basename "$1")")
  while [ "$LINK" ]; do
    cd "$(dirname "$LINK")"
    LINK=$(readlink "$(basename "$1")")
  done
  REALPATH="$PWD/$(basename "$1")"
  cd "$OURPWD"
  echo "$REALPATH"
}

TOOL_PATH=$(realpath (ToolPath))
FAKE="$TOOL_PATH"/fake

if ! [ -e "$FAKE" ]
then
#if (version == "latest")
  dotnet tool install fake-cli --tool-path "$TOOL_PATH" --prerelease --allow-downgrade
#else
  dotnet tool install fake-cli --tool-path "$TOOL_PATH" --version (version) --allow-downgrade
#endif
fi
"$FAKE" "$@"
