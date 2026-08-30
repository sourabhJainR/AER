# AER release checklist

## NuGet

Package: `AER.Format.Core`

```bash
dotnet pack src/Aer.Core/Aer.Core.csproj -c Release
```

## npm

Package: `@aer-format/core`

```bash
cd implementations/typescript
npm install
npm run build
npm publish --access public
```

## PyPI

Package: `aer-format`

```bash
cd implementations/python
python -m pip install --upgrade build twine
python -m build
python -m twine upload dist/*
```

## Go

Go modules are consumed directly from the repository/module path. Tag releases as `vMAJOR.MINOR.PATCH` after the AER-B conformance suite passes.

## Release gates

1. All language implementations compile.
2. All frozen AER-B vectors decode successfully.
3. Known encode vectors match byte-for-byte.
4. Text round-trip tests pass.
5. AI benchmark metadata is captured.
6. No benchmark claim is published without reproducible data.
7. Release tag and package versions match.
8. Security and license files are present.
