#!/usr/bin/env bash

usage()
{
    echo "Usage: $0 [managed] [native] [BuildArch] [BuildType] [clean] [verbose] [clangx.y] [platform] [cross] [skiptests] [staticLibLink] [cmakeargs] [makeargs]"
    echo "managed - optional argument to build the managed code"
    echo "native - optional argument to build the native code"
    echo "The following arguments affect native builds only:"
    echo "BuildArch can be: x64, x86, arm, arm-softfp, arm64"
    echo "BuildType can be: debug, release"
    echo "clean - optional argument to force a clean build."
    echo "verbose - optional argument to enable verbose build output."
    echo "clangx.y - optional argument to build using clang version x.y."
    echo "platform can be: FreeBSD, Linux, NetBSD, OSX, Windows"
    echo "cross - optional argument to signify cross compilation,"
    echo "      - will use ROOTFS_DIR environment variable if set."
    echo "skiptests - skip the tests in the './bin/*/*Tests/' subdirectory."
    echo "staticLibLink - Optional argument to statically link any native library."
    echo "generateversion - if building native only, pass this in to get a version on the build output."
    echo "cmakeargs - user-settable additional arguments passed to CMake."
    exit 1
}

setup_dirs()
{
    echo Setting up directories for build

    mkdir -p "$__BinDir"
    mkdir -p "$__IntermediatesDir"
}

# Performs "clean build" type actions (deleting and remaking directories)

clean()
{
    echo "Cleaning previous output for the selected configuration"
    rm -rf "$__BinDir"
    rm -rf "$__IntermediatesDir"
    setup_dirs
}

# Prepare the system for building

prepare_managed_build()
{
    # Run Init-Tools to restore BuildTools and ToolRuntime
    $__scriptpath/init-tools.sh
}

build_managed()
{
    __buildproj=$__scriptpath/build.proj
    __buildlog=$__scriptpath/msbuild.log

    $__scriptpath/Tools/dotnetcli/dotnet $__scriptpath/Tools/MSBuild.exe "$__buildproj" /m /nologo /verbosity:minimal "/flp:Verbosity=normal;LogFile=$__buildlog" "/flp2:warningsonly;logfile=$__scriptpath/msbuild.wrn" "/flp3:errorsonly;logfile=$__scriptpath/msbuild.err" /p:ConfigurationGroup=$__BuildType /p:TargetOS=$__BuildOS /p:OSGroup=$__BuildOS /p:SkipTests=$__SkipTests /p:COMPUTERNAME=$(hostname) /p:USERNAME=$(id -un) /p:TestNugetRuntimeId=$__TestNugetRuntimeId $__UnprocessedBuildArgs
    BUILDERRORLEVEL=$?

    echo

    # Pull the build summary from the log file
    tail -n 4 "$__buildlog"
    echo Build Exit Code = $BUILDERRORLEVEL
}

__scriptpath=$(cd "$(dirname "$0")"; pwd -P)
__packageroot=$__scriptpath/packages
__sourceroot=$__scriptpath/src
__rootbinpath="$__scriptpath/bin"
__generateversionsource=false
__buildmanaged=false
__TestNugetRuntimeId=win7-x64

# Use uname to determine what the CPU is.
CPUName=$(uname -p)
# Some Linux platforms report unknown for platform, but the arch for machine.
if [ $CPUName == "unknown" ]; then
    CPUName=$(uname -m)
fi

case $CPUName in
    i686)
        __BuildArch=x86
        ;;

    x86_64)
        __BuildArch=x64
        ;;

    armv7l)
        __BuildArch=arm
        ;;

    aarch64)
        __BuildArch=arm64
        ;;

    *)
        echo "Unknown CPU $CPUName detected, configuring as if for x64"
        __BuildArch=x64
        ;;
esac

# Use uname to determine what the OS is.
OSName=$(uname -s)
case $OSName in
    Darwin)
        __HostOS=OSX
        __TestNugetRuntimeId=osx.10.10-x64
        ;;

    FreeBSD)
        __HostOS=FreeBSD
        # TODO: Add native version
        __TestNugetRuntimeId=osx.10.10-x64
        ;;

    Linux)
        __HostOS=Linux
        if [ ! -e /etc/os-release ]; then
            echo "Cannot determine Linux distribution, assuming Ubuntu 14.04"
            __TestNugetRuntimeId=ubuntu.14.04-x64
        else
            source /etc/os-release
            __TestNugetRuntimeId=$ID.$VERSION_ID-$__BuildArch
        fi
        ;;

    NetBSD)
        __HostOS=NetBSD
        # TODO: Add native version
        __TestNugetRuntimeId=osx.10.10-x64
        ;;

    *)
        echo "Unsupported OS '$OSName' detected. Configuring as if for Ubuntu."
        __HostOS=Linux
        __TestNugetRuntimeId=ubuntu.14.04-x64
        ;;
esac
__BuildOS=$__HostOS
__BuildType=Debug
__CMakeArgs=DEBUG
__CMakeExtraArgs=""
__MakeExtraArgs=""

BUILDERRORLEVEL=0

# Set the various build properties here so that CMake and MSBuild can pick them up
__UnprocessedBuildArgs=
__CleanBuild=false
__CrossBuild=0
__SkipTests=false
__ServerGC=0
__VerboseBuild=false
__ClangMajorVersion=3
__ClangMinorVersion=5

while :; do
    if [ $# -le 0 ]; then
        break
    fi

    lowerI="$(echo $1 | awk '{print tolower($0)}')"
    case $lowerI in
        -\?|-h|--help)
            usage
            exit 1
            ;;
        managed)
            __buildmanaged=true
            ;;
        x86)
            __BuildArch=x86
            ;;
        x64)
            __BuildArch=x64
            ;;
        arm)
            __BuildArch=arm
            ;;
        arm-softfp)
            __BuildArch=arm-softfp
            ;;
        arm64)
            __BuildArch=arm64
            ;;
        debug)
            __BuildType=Debug
            ;;
        release)
            __BuildType=Release
            __CMakeArgs=RELEASE
            ;;
        clean)
            __CleanBuild=1
            ;;
        verbose)
            __VerboseBuild=1
            ;;
        staticliblink)
            __CMakeExtraArgs="$__CMakeExtraArgs -DCMAKE_STATIC_LIB_LINK=1"
            ;;
        generateversion)
            __generateversionsource=true
            ;;
        clang3.5)
            __ClangMajorVersion=3
            __ClangMinorVersion=5
            ;;
        clang3.6)
            __ClangMajorVersion=3
            __ClangMinorVersion=6
            ;;
        clang3.7)
            __ClangMajorVersion=3
            __ClangMinorVersion=7
            ;;
        clang3.8)
            __ClangMajorVersion=3
            __ClangMinorVersion=8
            ;;
        freebsd)
            __BuildOS=FreeBSD
            __TestNugetRuntimeId=osx.10.10-x64
            ;;
        linux)
            __BuildOS=Linux
            # If the Host OS is also Linux, then use the RID of the host.
            # Otherwise, override it to Ubuntu by default.
            if [ "$__HostOS" != "Linux" ]; then
                __TestNugetRuntimeId=ubuntu.14.04-x64
            fi
            ;;
        netbsd)
            __BuildOS=NetBSD
            __TestNugetRuntimeId=osx.10.10-x64
            ;;
        osx)
            __BuildOS=OSX
            __TestNugetRuntimeId=osx.10.10-x64
            ;;
        windows)
            __BuildOS=Windows_NT
            __TestNugetRuntimeId=win7-x64
            ;;
        cross)
            __CrossBuild=1
            ;;
        skiptests)
            __SkipTests=true
            ;;
        cmakeargs)
            if [ -n "$2" ]; then
                __CMakeExtraArgs="$__CMakeExtraArgs $2"
                shift
            else
                echo "ERROR: 'cmakeargs' requires a non-empty option argument"
                exit 1
            fi
            ;;
        makeargs)
            if [ -n "$2" ]; then
                __MakeExtraArgs="$__MakeExtraArgs $2"
                shift
            else
                echo "ERROR: 'makeargs' requires a non-empty option argument"
                exit 1
            fi
            ;;
        useservergc)
            __ServerGC=1
            ;;
        *)
            __UnprocessedBuildArgs="$__UnprocessedBuildArgs $1"
    esac

    shift
done

if [ "$__buildmanaged" = false ]; then
    __buildmanaged=true
fi

# Set the remaining variables based upon the determined build configuration
__IntermediatesDir="$__rootbinpath/obj/$__BuildOS.$__BuildArch.$__BuildType/Native"
__BinDir="$__rootbinpath/$__BuildOS.$__BuildArch.$__BuildType/Native"

# Make the directories necessary for build if they don't exist

setup_dirs

export CORECLR_SERVER_GC="$__ServerGC"

if $__buildmanaged; then

    # Prepare the system

    prepare_managed_build

    # Build the corefx native components.

    build_managed

    # Build complete
fi

# If managed build failed, exit with the status code of the managed build
if [ $BUILDERRORLEVEL != 0 ]; then
    exit $BUILDERRORLEVEL
fi

exit $BUILDERRORLEVEL
