using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MikaSourceGen
{
    // 제너레이터 본체: 파이프라인 구성과 공용 헬퍼 담당.
    // 실제 코드 생성(Extract/Emit)은 책임별로 partial 파일에 분리한다.
    //   - PacketHandlerGenerator.Handlers.cs  : [PacketHandler] -> GeneratedHandlers.g.cs   (서버/클라에서 생성)
    //   - PacketHandlerGenerator.PacketIds.cs : [Packet]        -> GeneratedPacketIds.g.cs  (MikaProtocol에서 생성)
    [Generator(LanguageNames.CSharp)]
    public sealed partial class PacketHandlerGenerator : IIncrementalGenerator
    {
        private const string HandlerAttr = "MikaNetwork.PacketHandlerAttribute";
        private const string PacketAttr = "MikaNetwork.PacketAttribute";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // [PacketHandler] 붙은 메서드 -> 핸들러 등록 테이블
            var handlers = context.SyntaxProvider.ForAttributeWithMetadataName(
                    HandlerAttr,
                    predicate: static (node, _) => node is MethodDeclarationSyntax,
                    transform: static (ctx, _) => ExtractHandler(ctx))
                .Where(static h => h is not null)
                .Select(static (h, _) => h!.Value)
                .Collect();

            // [Packet] 붙은 타입 -> 타입↔Id 매핑 테이블
            var packetIds = context.SyntaxProvider.ForAttributeWithMetadataName(
                    PacketAttr,
                    predicate: static (node, _) => node is TypeDeclarationSyntax,
                    transform: static (ctx, _) => ExtractPacketId(ctx))
                .Where(static p => p is not null)
                .Select(static (p, _) => p!.Value)
                .Collect();

            // 서로 다른 소스로 구동 -> 각 파일이 알맞은 프로젝트에서 독립적으로 생성·캐싱된다.
            context.RegisterSourceOutput(handlers, EmitHandlers);
            context.RegisterSourceOutput(packetIds, EmitPacketIds);
        }

        // enum 상수(TypedConstant) -> "MikaProtocol.PacketId.C_EchoRequest" 같은 참조 문자열
        static string ResolveIdRef(TypedConstant idConst)
        {
            var idName = (idConst.Type as INamedTypeSymbol)?.GetMembers()
                .OfType<IFieldSymbol>()
                .FirstOrDefault(f => f.HasConstantValue && Equals(f.ConstantValue, idConst.Value))
                ?.Name;

            return idName is null
                ? idConst.Value!.ToString()                                  // fallback: 숫자 리터럴
                : $"{idConst.Type!.ToDisplayString()}.{idName}";             // 예: MikaProtocol.PacketId.C_EchoRequest
        }
    }
}
