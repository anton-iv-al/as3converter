namespace FHelpers

open System.Text.RegularExpressions

module RegexF =
    let replaceFunc pattern f str = 
        Regex.Replace(str, pattern, new MatchEvaluator(f) )
        
    let replaceTemplate pattern (replacement:string) str = 
        Regex.Replace(str, pattern, replacement)
        
        
    let closingBraceIndex (src:string) startIndex (leftBrace :char) (rightBrace:char) =
        let folder counter a = 
            match a with
            | a when a = leftBrace  -> counter + 1  
            | a when a = rightBrace -> counter - 1  
            | _                     -> counter  
        src 
        |> Seq.skip startIndex
        |> Seq.scan folder 0 |> Seq.tail
        |> Seq.tryFindIndex ((=) 0)
        |> function
                  | Some i -> Some (i + startIndex)
                  | None -> None   
        
    
    let blockStartsWithLastBrace src (m:Match) = 
        let openBlockIndex = m.Index + m.Length - 1
        closingBraceIndex src openBlockIndex '{' '}'
        |> function 
           | None -> ""
           | Some closingBlockIndex ->
                src.Substring(openBlockIndex, closingBlockIndex - openBlockIndex + 1)  
            
            
    let replaceGroups replacers (m:Match) =
        let groups = Seq.cast<Capture> m.Groups |> Seq.tail
        
        let replaceGroup (res:seq<string>) (replacer, (g:Capture)) =             
            let startIndex = g.Index - m.Index
            
            let before      = (Seq.head res).Substring(0, startIndex)
            let after       = (Seq.head res).Substring(startIndex + g.Length)
            let replacement = (Seq.head res).Substring(startIndex, g.Length) |> replacer
            
            before :: replacement :: after :: List.ofSeq (Seq.tail res)
        
        let toConcat = Seq.fold replaceGroup [m.Value] (Seq.zip replacers groups |> Seq.rev)
        System.String.Join("", toConcat)
        
        
        
module StringF =
    let replace (oldVal:string) (newVal:string) (str:string) = str.Replace(oldVal, newVal)
    let join (sep:string) (toJoin:string list) = System.String.Join(sep, toJoin)    
    
    
module Seq =
    let merge seq1 seq2 = Seq.map2 (fun a b -> [a;b]) seq1 seq2 |> Seq.concat
    
    
    
module FSharpExtension = 
    let self a = a
    let flip f = fun a b -> f b a    