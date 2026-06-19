using MikaServerCore.Network;
using Shouldly;

namespace MikaServerCore.test;

public class MikaRecvBufferTests
{
    private static void Write(MikaRecvBuffer buffer, byte[] data)
    {
        var memory = buffer.GetWritableMemory(data.Length);
        data.CopyTo(memory);
        buffer.AdvanceWrite(data.Length);
    }

    /// <summary>쓴 데이터를 읽으면 같은 길이·같은 바이트가 그대로 나오는지 확인한다.</summary>
    [Fact]
    public void Write_Then_Read_Returns_Same_Data()
    {
        var buffer = new MikaRecvBuffer(16);
        byte[] data = [1, 2, 3, 4, 5];

        Write(buffer, data);

        buffer.ReadableBytes.ShouldBe(5);
        buffer.GetReadableSpan().ToArray().ShouldBe(data);
    }

    /// <summary>버퍼를 전부 읽고 나면 read/write offset이 0으로 리셋되어 전체가 다시 쓰기 가능해지는지 확인한다.</summary>
    [Fact]
    public void Offsets_Reset_After_Reading_Everything()
    {
        var buffer = new MikaRecvBuffer(16);
        Write(buffer, [1, 2, 3, 4, 5]);

        buffer.AdvanceRead(5);

        buffer.ReadableBytes.ShouldBe(0);
        // 전부 읽으면 read/write offset이 0으로 리셋되어 버퍼 전체가 다시 쓰기 가능해야 한다
        buffer.WritableBytes.ShouldBe(16);
    }

    /// <summary>일부만 읽었을 때 읽지 않은 나머지 데이터가 올바른 위치에 보존되는지 확인한다.</summary>
    [Fact]
    public void Partial_Read_Keeps_Remaining_Data()
    {
        var buffer = new MikaRecvBuffer(16);
        Write(buffer, [1, 2, 3, 4, 5]);

        buffer.AdvanceRead(2);

        buffer.ReadableBytes.ShouldBe(3);
        buffer.GetReadableSpan().ToArray().ShouldBe(new byte[] { 3, 4, 5 });
    }

    /// <summary>writable 공간이 모자라 버퍼가 2배로 성장할 때, 기존 미읽기 데이터가 손실 없이 컴팩션되는지 확인한다.</summary>
    [Fact]
    public void Grow_Preserves_Unread_Data()
    {
        var buffer = new MikaRecvBuffer(8);
        Write(buffer, [1, 2, 3, 4, 5, 6]);
        buffer.AdvanceRead(2); // 남은 데이터: [3,4,5,6], writable 2바이트

        // writable(2) < 요청(10) → 버퍼 성장 + 컴팩션이 일어나야 한다
        Write(buffer, [7, 8, 9, 10, 11, 12, 13, 14, 15, 16]);

        buffer.ReadableBytes.ShouldBe(14);
        buffer.GetReadableSpan().ToArray()
            .ShouldBe(new byte[] { 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 });
    }

    /// <summary>전체 필요량이 기존 버퍼 크기 이하라 성장 없이 컴팩션만으로 공간을 확보할 때도 데이터가 보존되는지 확인한다.</summary>
    [Fact]
    public void Compaction_Without_Growth_Preserves_Unread_Data()
    {
        var buffer = new MikaRecvBuffer(8);
        Write(buffer, [1, 2, 3, 4, 5, 6]);
        buffer.AdvanceRead(4); // 남은 데이터: [5,6], writable 2바이트

        // 전체 필요량(2+4=6)은 기존 크기(8) 이하 → 컴팩션만으로 공간 확보
        Write(buffer, [7, 8, 9, 10]);

        buffer.ReadableBytes.ShouldBe(6);
        buffer.GetReadableSpan().ToArray().ShouldBe(new byte[] { 5, 6, 7, 8, 9, 10 });
    }

    /// <summary>버퍼 크기와 정확히 같은 양을 요청해도 예외 없이 해당 크기의 쓰기 공간을 돌려주는지(경계값) 확인한다.</summary>
    [Fact]
    public void Exact_Fit_Does_Not_Throw()
    {
        var buffer = new MikaRecvBuffer(8);

        var memory = buffer.GetWritableMemory(8);

        memory.Length.ShouldBe(8);
    }
}
