using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;

namespace Benchmark.System;

[Config(typeof(LocalConfig))]
public class SubCmpAssembly
{
    private class LocalConfig : ManualConfig
    {
        public LocalConfig()
        {
            AddJob(Job.Dry.WithEnvironmentVariable("DOTNET_TC_QuickJit", "0"));
            AddDiagnoser(new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig(printInstructionAddresses: true)));
            Orderer = new DefaultOrderer(SummaryOrderPolicy.Declared, MethodOrderPolicy.Declared);
        }
    }

    // Trying to produce something like:
    // SUB
    // Jcc
    // MOV
    // CMP
    // SETcc

    [Benchmark]
    [Arguments(16, 7, 7)]
    public bool SubCollapsedToJump(long lengthInner, long offsetInput, int length)
    {
        // GCC produces the optimal:
        // SUB    rdi, rsi
        // JB     .L3
        // MOV    edx, edx
        // CMP    rdx, rdi
        // SETBE  al
        // RET
        // .L3:
        // XOR    eax, eax
        // RET

        // Jcc should be modified to use SUB if this optimization is present
        var remaining = (ulong)lengthInner - (ulong)offsetInput;
        return remaining <= (ulong)lengthInner && remaining >= (uint)length;
    }

    [Benchmark]
    [Arguments(16, 7, 7)]
    public bool CmpConvertedToSubJump(long lengthInner, long offsetInput, int length)
    {
        // CMP should be converted to SUB if this optimization is present
        return (ulong)lengthInner >= (ulong)offsetInput && (ulong)lengthInner - (ulong)offsetInput >= (uint)length;
    }

    [Benchmark]
    [Arguments(16, 7, 7)]
    public bool CmpElidedForSubJump(long lengthInner, long offsetInput, int length)
    {
        // CMP should be elided in favor of SUB if this optimization is present
        var remaining = (ulong)lengthInner - (ulong)offsetInput;
        return (ulong)lengthInner >= (ulong)offsetInput && remaining >= (uint)length;
    }

    
}
