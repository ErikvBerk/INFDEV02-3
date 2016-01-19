﻿module CommonLatex

let beginFrame = @"\begin{frame}[fragile]{\CurrentSection}" + "\n"
let endFrame = "\n" + @"\end{frame}" + "\n\n"
let beginBlock = @"\begin{block}{\CurrentSubSection}" + "\n"
let endBlock = "\n" + @"\end{block}" + "\n\n"
let beginExampleBlock = @"\begin{exampleblock}{}" + "\n"
let endExampleBlock = "\n" + @"\end{exampleblock}" + "\n\n"
let beginItemize = @"\begin{itemize}" + "\n"
let endItemize = "\n" + @"\end{itemize}" + "\n"
let beginCode = @"\begin{lstlisting}" + "\n"
let endCode = "\n" + @"\end{lstlisting}" + "\n"
let beginMath = @"$$"
let endMath = @"$$" + "\n"

let beginTabular c = 
  match c with
  | [] -> @"\begin{tabular}{}" + "\n\\hline"
  | _ -> @"\begin{tabular}{ | " + (c |> List.reduce (fun a b -> a + " | " + b))  + " | }\n\\hline"
let endTabular = @"\end{tabular}"
