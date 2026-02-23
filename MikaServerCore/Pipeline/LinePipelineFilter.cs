using System.Buffers;
using System.Text;

namespace MikaServerCore.Pipeline;

public class LinePipelineFilter : IPipelineFilter<TextPackageInfo>
{

    public bool TryDecode(ref ReadOnlySequence<byte> buffer, out TextPackageInfo? package)
    {
        var reader = new SequenceReader<byte>(buffer);
        
        if(!reader.TryReadTo(out ReadOnlySequence<byte> line, (byte)'\n'))
        {
            package = null;
            return false;
        }

        buffer = buffer.Slice(reader.Position);

        var text = Encoding.UTF8.GetString(line.ToArray());
        if (text.EndsWith('\r'))
            text = text[..^1];
        
        package = new TextPackageInfo(text);
        return true;
    }
}