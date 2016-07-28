# core-compose

This repo is used to validate a private coreclr build in CLI.

This repo is currently only validated on Windows 10. Validation on Linux is pending...

PreRequisites
The 1.0.0 shared runtime is required to build this repo (this repo builds core-setup which has a requirement on this version of the runtime).  This version of the runtime does not come with build tools.  To obtain this version of the runtime
  1. init-tools.cmd (install buildtools)
  2. Linux only
     - (cli repo)scripts/obtain/dotnet-install.sh -Version 1.0.0 -Architecture x64 -Channel preview -SharedRuntime -InstallDir (core-compose repo)Tools/dotnetcli

To use this repro,

- Clone core-compose local
- Clone coreclr repo local
- Clone core-setup (release/1.0.0 branch) repo local
- Clone cli
- Modify config.props to point to your local repos
- Build coreclr repo (Release)
- from the root of the core-compose repo, run build.cmd

Note, shared framework version 1.0.0 is required, if you don't have it, you can obtain it by running https://github.com/dotnet/cli/blob/rel/1.0.0/scripts/obtain/dotnet-install.ps1 and setting the Version parameter as well as the InstallDir parameter (ToDo: provide install directory location, I think it's core-compose\tools\dotnetcli) 

Note, make sure your core-setup and cli repo's are clean before building or you may not get the version of coreclr that you expect.
