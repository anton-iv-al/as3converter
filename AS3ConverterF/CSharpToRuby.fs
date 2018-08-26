namespace AS3ConverterF

open FHelpers
open FSharpExtension

open System
open System.Text.RegularExpressions

module CSharpToRuby =    

    let private convertKeyWords = 
        StringF.replace "continue;" "next" >>
        StringF.replace ";" "" >>
        StringF.replace "//" "#"
        
    
    let private convertVars =
        let unityPattern = @"[\w+.\[\]\<\>]+\s+(\w+)\s*="
        let rubyPattern = @"$1 ="
        RegexF.replaceTemplate unityPattern rubyPattern
        
        
    let private convertForEach =
        let unityPattern = @"foreach\s*\(\s*[\w+.\[\]\<\>]+\s+(\w+)\s+in\s([\w.\[\]]+)\s*\)\s*\{"
        let rubyPattern = @"$2.each { |$1|"
        RegexF.replaceTemplate unityPattern rubyPattern
        
        
    let rec private convertIfWithoutBraces =
        let unityPattern = @"if\s*\((.*)\)(.*);"
        let rubyPattern = @"$2 if $1"
        RegexF.replaceTemplate unityPattern rubyPattern
            
            
    let rec private convertIfWithBraces src =
        let pattern = @"if\s*\((.*)\)\s*\{"
        
        let m = Regex.Match(src, pattern)
        
        if not m.Success 
        then src
        else        
            let matchReplacer = String.Format("if {0}", m.Groups.[1].Value)
            
            let block = RegexF.blockStartsWithLastBrace src m
            let blockReplacer = 
                let withoutBraces = 
                    match block with
                    | "" -> ""
                    | block ->
                        block
                        |> Seq.tail
                        |> Seq.rev |> Seq.tail |> Seq.rev
                        |> Array.ofSeq |> System.String
                withoutBraces + "end" 
                
            [
                src.Substring(0, m.Index)
                matchReplacer
                blockReplacer
                convertIfWithBraces(src.Substring(m.Index + m.Length + block.Length - 1))
            ]
            |> StringF.join ""
            
            
    let rec private convertElse src =
        let pattern = @"end\s*else\s*{"
        
        let m = Regex.Match(src, pattern)
        
        if not m.Success 
        then src
        else        
            let matchReplacer = "else"
            
            let block = RegexF.blockStartsWithLastBrace src m
            let blockReplacer = 
                let withoutBraces = 
                    match block with
                    | "" -> ""
                    | block ->
                        block
                        |> Seq.tail
                        |> Seq.rev |> Seq.tail |> Seq.rev
                        |> Array.ofSeq |> System.String
                withoutBraces + "end" 
                
            [
                src.Substring(0, m.Index)
                matchReplacer
                blockReplacer
                convertElse(src.Substring(m.Index + m.Length + block.Length - 1))
            ]
            |> StringF.join ""
            
            
    let rec private convertIf =
        convertIfWithoutBraces >>
        convertIfWithBraces >>
        convertElse

    
    let private convertFunctionParams =
        let unityPattern = @"[\w+.\[\]\<\>]+\s+(\w+)"
        let rubyPattern = @"$1"
        RegexF.replaceTemplate unityPattern rubyPattern
    
        
    let rec private convertFunctions src =
        let pattern = @"[\s\w]*\s+[\w+.\[\]\<\>]+\s+(\w+)\s*\((.*)\)\s*\{"
        
        let m = Regex.Match(src, pattern)
        
        if not m.Success 
        then src
        else
            let matchReplacer = String.Format("\ndef {0} {1}", m.Groups.[1].Value, convertFunctionParams m.Groups.[2].Value)
            
            let block = RegexF.blockStartsWithLastBrace src m
            let blockReplacer = 
                let withoutBraces = 
                    match block with
                    | "" -> ""
                    | block ->
                        block
                        |> Seq.tail
                        |> Seq.rev |> Seq.tail |> Seq.rev
                        |> Array.ofSeq |> System.String
                withoutBraces + "end" 
                
            [
                src.Substring(0, m.Index)
                matchReplacer
                blockReplacer
                convertFunctions(src.Substring(m.Index + m.Length + block.Length - 1))
            ]
            |> StringF.join ""
            
            
    let convertToSnakeCase src =
        let replace1 = 
            let unityPattern = @"([A-Z]+)([A-Z][a-z])"
            let rubyPattern = @"$1_$2"
            RegexF.replaceTemplate unityPattern rubyPattern
            
        let replace2 = 
            let unityPattern = @"([a-z\d])([A-Z])"
            let rubyPattern = @"$1_$2"
            RegexF.replaceTemplate unityPattern rubyPattern
            
        (src |> replace1 |> replace2).ToLower()
        
        
    let rec private convertSwitchBlock src =
        let pattern = @"switch\s*\(\s*([\w.\[\]]+)\s*\)\s*{"
        
        let m = Regex.Match(src, pattern)
        
        if not m.Success 
        then src
        else
            let matchReplacer = String.Format("\ncase {0}", m.Groups.[1].Value)
            
            let block = RegexF.blockStartsWithLastBrace src m
            let blockReplacer = 
                let withoutBraces = 
                    match block with
                    | "" -> ""
                    | block ->
                        block
                        |> Seq.tail
                        |> Seq.rev |> Seq.tail |> Seq.rev
                        |> Array.ofSeq |> System.String
                withoutBraces + "end" 
                
            [
                src.Substring(0, m.Index)
                matchReplacer
                blockReplacer
                convertSwitchBlock(src.Substring(m.Index + m.Length + block.Length - 1))
            ]
            |> StringF.join ""
                 
        
    let convertSwitch =
        let caseReplace = 
            let unityPattern = @"case\s+([\w.\[\]]+)\s*:"
            let rubyPattern = @"when $1"
            RegexF.replaceTemplate unityPattern rubyPattern
                            
        caseReplace >> convertSwitchBlock
    
        
    let convert src =
        src
        |> convertVars
        |> convertForEach
        |> convertIf
        |> convertSwitch
        |> convertFunctions
        |> convertKeyWords
        |> convertToSnakeCase
//        |> convertProperties
        
    

