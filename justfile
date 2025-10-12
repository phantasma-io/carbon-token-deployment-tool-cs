[private]
just:
    just -l

[group('run')]
r:
    cd src && dotnet run
