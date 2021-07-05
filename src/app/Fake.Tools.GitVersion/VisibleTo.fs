
namespace System
open System.Runtime.CompilerServices

[<assembly: InternalsVisibleTo("Fake.Core.IntegrationTests")>]
[<assembly: InternalsVisibleTo("Fake.Core.UnitTests")>]
do ()