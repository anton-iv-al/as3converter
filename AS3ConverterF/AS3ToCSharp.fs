namespace AS3ConverterF

open FHelpers
open FSharpExtension

open System
open System.Text.RegularExpressions

module AS3ToCSharp =

    let private convertKeyWords = 
        StringF.replace ":Number" ":double" >>
        StringF.replace ":Boolean" ":bool" >>
        StringF.replace ":String" ":string" >>
        StringF.replace "[Inline] " "" >>
        StringF.replace "for each" "foreach"
            
            
    let private convertVars =
        let as3Pattern = @"var\s+(\w+)\s*:\s*(\w+)"
        let unityPattern = @"$2 $1"
        RegexF.replaceTemplate as3Pattern unityPattern
    
    
    let private convertSplit =
        let as3Pattern = "split\\([\"'](.)[\"']\\)"
        let unityPattern = @"Split('$1')"
        RegexF.replaceTemplate as3Pattern unityPattern
    
    
    let private convertFunctionParams =
        let as3Pattern = @"(\w+):(\w+)"
        let unityPattern = @"$2 $1"
        RegexF.replaceTemplate as3Pattern unityPattern
        
        
    let private convertFunctions =
        let as3Pattern = @"function\s+(\w+)\s*(\(.*\))\s*:\s*(\w+)"
        let unityPattern = @"$3 $1$2"
        
        RegexF.replaceFunc as3Pattern (RegexF.replaceGroups [self; convertFunctionParams; self]) >>
        RegexF.replaceTemplate as3Pattern unityPattern


    


    let rec private convertPropertiesByOrder (key1:string) (key2:string option) src =
        let pattern1 = System.String.Format(@"([\s\w]*)\s+function\s+{0}\s+(\w+)\s*\(.*?\)\s*:\s*(\w+)\s*{{", key1)
        
        let m1 = Regex.Match(src, pattern1)
        
        if not m1.Success
        then src
        else
            let fModifier = m1.Groups.[1].Value
            let fName = m1.Groups.[2].Value
            let fType = m1.Groups.[3].Value
            
            let block1 = RegexF.blockStartsWithLastBrace src m1
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
                |> StringF.join ""
                |> convertPropertiesByOrder key1 key2
                                    
            | Some key2 ->
                let pattern2 = System.String.Format(@"([\s\w]*)\s+function\s+{0}\s+{1}\s*\(.*?\)\s*:\s*(\w+)\s*{{", key2, fName)
                let m2 = Regex.Match(srcAfterBlock1, pattern2)
    
                if not m2.Success
                then src.Substring(0, block1End) + (convertPropertiesByOrder key1 (Some key2) srcAfterBlock1)
                else
                    let block2 = RegexF.blockStartsWithLastBrace srcAfterBlock1 m2
                    let block2ToReplace = "\n " + key2 + block2
                    
                    let srcAfterBlock2 = srcAfterBlock1.Substring(m2.Index + m2.Length + block2.Length - 1)
    
                    let replacer = System.String.Format("{0} {1} {2} \n {{ {3} {4} \n }}", fModifier, fType, fName, block1ToReplace, block2ToReplace )
                    
                    [
                        src.Substring(0, m1.Index)
                        replacer
                        srcAfterBlock1.Substring(0, m2.Index)
                        srcAfterBlock2
                    ]
                    |> StringF.join ""
                    |> convertPropertiesByOrder key1 (Some key2)
                    
    
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
    
    

