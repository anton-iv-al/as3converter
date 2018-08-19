namespace AS3ConverterF

open System
open System.Text.RegularExpressions

module private Seq =
    let merge seq1 seq2 = Seq.map2 (fun a b -> [a;b]) seq1 seq2 |> Seq.concat

module AS3ToSharp =

    type Regex with
        static member internal ReplaceFunc pattern f str = 
            Regex.Replace(str, pattern, new MatchEvaluator(f) )
            
        static member internal ReplaceTemplate pattern (replacement:string) str = 
            Regex.Replace(str, pattern, replacement)
        
    type System.String with
        static member internal replace (oldVal:string) (newVal:string) (str:string) = str.Replace(oldVal, newVal)
        static member internal join (sep:string) (toJoin:string list) = System.String.Join(sep, toJoin)
    
    let private stub a = a
    
    
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
    
        
    let private replaceGroups replacers (m:Match) =
        let groups = Seq.cast<Capture> m.Groups |> Seq.tail
        
        let replaceGroup (res:seq<string>) (replacer, (g:Capture)) =             
            let startIndex = g.Index - m.Index
            
            let before      = (Seq.head res).Substring(0, startIndex)
            let after       = (Seq.head res).Substring(startIndex + g.Length)
            let replacement = (Seq.head res).Substring(startIndex, g.Length) |> replacer
            
            before :: replacement :: after :: List.ofSeq (Seq.tail res)
        
        let toConcat = Seq.fold replaceGroup [m.Value] (Seq.zip replacers groups |> Seq.rev)
        System.String.Join("", toConcat)
            
        
    let private convertKeyWords = 
        String.replace ":Number" ":double" >>
        String.replace ":Boolean" ":bool" >>
        String.replace ":String" ":string" >>
        String.replace "[Inline] " "" >>
        String.replace "for each" "foreach"
            
            
    let private convertVars =
        let as3Pattern = @"var\s+(\w+)\s*:\s*(\w+)"
        let unityPattern = @"$2 $1"
        Regex.ReplaceTemplate as3Pattern unityPattern
    
    
    let private convertSplit =
        let as3Pattern = "split\\([\"'](.)[\"']\\)"
        let unityPattern = @"Split('$1')"
        Regex.ReplaceTemplate as3Pattern unityPattern
    
    
    let private convertFunctionParams =
        let as3Pattern = @"(\w+):(\w+)"
        let unityPattern = @"$2 $1"
        Regex.ReplaceTemplate as3Pattern unityPattern
        
        
    let private convertFunctions =
        let as3Pattern = @"function\s+(\w+)\s*(\(.*\))\s*:\s*(\w+)"
        let unityPattern = @"$3 $1$2"
        
        Regex.ReplaceFunc as3Pattern (replaceGroups [stub; convertFunctionParams; stub]) >>
        Regex.ReplaceTemplate as3Pattern unityPattern


    let propertyBlock src (m:Match) = 
        let openBlockIndex = m.Index + m.Length - 1
        closingBraceIndex src openBlockIndex '{' '}'
        |> function 
           | None -> ""
           | Some closingBlockIndex ->
                src.Substring(openBlockIndex, closingBlockIndex - openBlockIndex + 1)     


    let rec private convertPropertiesByOrder (key1:string) (key2:string option) src =
        let pattern1 = System.String.Format(@"([\s\w]*)\s+function\s+{0}\s+(\w+)\s*\(.*?\)\s*:\s*(\w+)\s*{{", key1)
        
        let m1  = Regex.Match(src, pattern1)
        
        if not m1.Success
        then src
        else
            let fModifier = m1.Groups.[1].Value
            let fName = m1.Groups.[2].Value
            let fType = m1.Groups.[3].Value
            
            let block1 = propertyBlock src m1
            let block1ToReplace = "\n " + key1 + block1
            
            let block1End = m1.Index + m1.Length + block1.Length - 1
            let srcAfterBlock1 = src.Substring(block1End)
            
            match key2 with
            | None -> 
                let replacer = System.String.Format("{0} {1} {2} \n {{ {3} \n }}", fModifier, fType, fName, block1ToReplace )
                [
                    src.Substring(0, m1.Index)
                    replacer
                    srcAfterBlock1
                ]
                |> System.String.join ""
                |> convertPropertiesByOrder key1 key2
                                    
            | Some key2 ->
                let pattern2 = System.String.Format(@"([\s\w]*)\s+function\s+{0}\s+{1}\s*\(.*?\)\s*:\s*(\w+)\s*{{", key2, fName)
                let m2 = Regex.Match(srcAfterBlock1, pattern2)
    
                if not m2.Success
                then src.Substring(0, block1End) + (convertPropertiesByOrder key1 (Some key2) srcAfterBlock1)
                else
                    let block2 = propertyBlock srcAfterBlock1 m2
                    let block2ToReplace = "\n " + key2 + block2
                    
                    let srcAfterBlock2 = srcAfterBlock1.Substring(m2.Index + m2.Length + block2.Length - 1)
    
                    let replacer = System.String.Format("{0} {1} {2} \n {{ {3} {4} \n }}", fModifier, fType, fName, block1ToReplace, block2ToReplace )
                    
                    [
                        src.Substring(0, m1.Index)
                        replacer
                        srcAfterBlock1.Substring(0, m2.Index)
                        srcAfterBlock2
                    ]
                    |> System.String.join ""
                    |> convertPropertiesByOrder key1 (Some key2)
                    
// обработать static

    
    let private convertProperties =
        convertPropertiesByOrder "get" (Some "set") >>
        convertPropertiesByOrder "set" (Some "get") >>
        convertPropertiesByOrder "get" None >>
        convertPropertiesByOrder "set" None
        
        
    let convert src =
        src
        |> convertKeyWords
        |> convertSplit
        |> convertVars
        |> convertFunctions
        |> convertProperties
    
    

