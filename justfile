[private]
just:
    just -l

[group('run')]
r:
    cd src && dotnet run

[group('manage')]
nuget-clear-cache:
    dotnet nuget locals all --clear
