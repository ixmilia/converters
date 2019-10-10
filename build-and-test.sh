#!/usr/bin/env bash

source="${BASH_SOURCE[0]}"

# resolve $SOURCE until the file is no longer a symlink
while [[ -h $source ]]; do
  scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"
  source="$(readlink "$source")"

  # if $source was a relative symlink, we need to resolve it relative to the path where the
  # symlink file was located
  [[ $source != /* ]] && source="$scriptroot/$source"
done

scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"

# IxMilia.Dxf needs a custom invocation to generate code
./src/IxMilia.Dxf/build-and-test.sh --notest

SOLUTION=$scriptroot/IxMilia.Converters.sln
dotnet restore $SOLUTION
dotnet build $SOLUTION
dotnet test $SOLUTION
