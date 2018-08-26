open System.Windows
open AS3ConverterF

[<EntryPoint>]
[<System.STAThread>]
let main argv = 
    Clipboard.SetData(DataFormats.Text, AS3ToCSharp.convert (Clipboard.GetText()))
    0 // return an integer exit code
