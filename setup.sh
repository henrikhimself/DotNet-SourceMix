#!/usr/bin/env sh
dotnet tool uninstall --global HenrikJensen.SourceMix \
    && ./scripts/pack.bash \
    && dotnet tool install --global --add-source ./src/SourceMix/bin/Release HenrikJensen.SourceMix --prerelease
