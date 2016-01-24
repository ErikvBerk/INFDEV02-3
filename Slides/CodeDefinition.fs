﻿module CodeDefinition
open Coroutine
open CommonLatex

type Operator = Plus | Minus | Times | DividedBy | GreaterThan
  with
    member this.AsPython =
      match this with
      | Plus -> "+"
      | Minus -> "-"
      | Times -> "*"
      | DividedBy -> "/"
      | GreaterThan -> ">"
    member this.AsCSharp =
      match this with
      | _ -> this.AsPython

let (!+) = List.fold (+) ""

type Code =
  | End
  | None
  | Ref of string
  | Object of Map<string, Code>
  | New of string * List<Code>
  | ClassDef of string * List<Code>
  | Return of Code
  | TypedDecl of string * string * Option<Code>
  | Var of string
  | Hidden of Code
  | ConstLambda of int * List<string> * Code
  | ConstBool of bool
  | ConstInt of int
  | ConstFloat of float
  | ConstString of string
  | Assign of string * Code
  | TypedDef of string * List<string * string> * string * Code
  | Def of string * List<string> * Code
  | Call of string * List<Code>
  | MethodCall of string * string * List<Code>
  | StaticMethodCall of string * string * List<Code>
  | If of Code * Code * Code
  | While of Code * Code
  | Op of Code * Operator * Code
  | Sequence of Code * Code
  with 
    member this.AsPython pre = 
      match this with
      | Object bs ->
        let argss = bs |> Map.remove "__type" |> Seq.map (fun a -> a.Key + "=" + (a.Value.AsPython "") + ", ") |> Seq.toList
        sprintf "%s%s" pre ((!+argss).TrimEnd[|','; ' '|])
      | End -> ""
      | None -> "None"
      | ClassDef(s,ms) -> 
        let mss = ms |> List.map (fun m -> m.AsPython (pre + "  ") + "\n")
        sprintf "class %s:\n%s" s !+mss
      | Return c ->
        sprintf "%sreturn %s\n" pre ((c.AsPython "").Replace("\n",""))
      | Var s -> s
      | ConstBool b -> b.ToString()
      | ConstInt i -> i.ToString()
      | ConstFloat f -> f.ToString()
      | ConstString s -> sprintf "\"%s\"" s
      | Ref s -> sprintf "ref %s" s
      | Assign (v,c) -> sprintf "%s%s = %s\n" pre v ((c.AsPython "").TrimEnd([|'\n'|]))
      | ConstLambda (pc,args,body) ->
        let argss = args |> List.map (fun a -> a + ",")
        sprintf "%slambda(%s): %s" pre ((!+argss).TrimEnd[|','|]) (body.AsPython (pre + "  "))
      | Def (n,args,body) ->
        let argss = args |> List.map (fun a -> a + ",")
        sprintf "%sdef %s(%s):\n%s" pre n ((!+argss).TrimEnd[|','|]) (body.AsPython (pre + "  "))
      | New(c,args) ->
        let argss = args |> List.map (fun a -> (a.AsPython "").TrimEnd([|'\n'|]) + ",")
        sprintf "%s%s(%s)\n" pre c ((!+argss).TrimEnd[|','|])
      | Call(n,args) ->
        let argss = args |> List.map (fun a -> (a.AsPython "").TrimEnd([|'\n'|]) + ",")
        sprintf "%s%s(%s)\n" pre n ((!+argss).TrimEnd[|','|])
      | MethodCall(n,m,args) ->
        let argss = args |> List.map (fun a -> (a.AsPython "").TrimEnd([|'\n'|]) + ",")
        sprintf "%s%s.%s(%s)\n" pre n m ((!+argss).TrimEnd[|','|])
      | StaticMethodCall(c,m,args) ->
        let argss = args |> List.map (fun a -> (a.AsPython "").TrimEnd([|'\n'|]) + ",")
        sprintf "%s%s.%s(%s)\n" pre c m ((!+argss).TrimEnd[|','|])
      | If(c,t,e) ->
        let tS = (t.AsPython (pre + "  "))
        sprintf "%sif %s:\n%s%selse:\n%s" pre (c.AsPython "") tS pre (e.AsPython (pre + "  "))
      | While(c,b) ->
        let bs = (b.AsPython (pre + "  "))
        sprintf "%swhile %s:\n%s" pre (c.AsPython "") bs
      | Op(a,op,b) ->
        sprintf "%s %s %s" ((a.AsPython "").Replace("\n","")) (op.AsPython) ((b.AsPython (pre + "  ")).Replace("\n",""))
      | Sequence (p,q) ->
        let res = sprintf "%s%s" (p.AsPython pre) (q.AsPython pre)
        res
      | Hidden(_) -> ""
      | _ -> failwith "Unsupported Python statement"
    member this.NumberOfPythonLines = 
      let code = ((this.AsPython ""):string).TrimEnd([|'\n'|])
      let lines = code.Split([|'\n'|])
      lines.Length

    member this.AsCSharp pre = 
      match this with
      | End -> ""
      | None -> "void"
      | ClassDef(s,ms) -> 
        let mss = ms |> List.map (fun m -> m.AsCSharp (pre + "  "))
        let res = sprintf "class %s {\n%s%s}\n" s (!+mss) pre
        res
      | Return c ->
        sprintf "%sreturn %s;\n" pre ((c.AsCSharp "").TrimEnd[|','; '\n'; ';'|])
      | TypedDecl(s,t,Option.None) -> 
        if t = "" then sprintf "%s%s;\n" pre s
        else sprintf "%s%s %s;\n" pre t s
      | TypedDecl(s,t,Some v) -> 
        if t = "" then sprintf "%s%s = %s;\n" pre s ((v.AsCSharp "").TrimEnd[|','; '\n'; ';'|])
        else sprintf "%s%s %s = %s;\n" pre t s ((v.AsCSharp "").TrimEnd[|','; '\n'; ';'|])
      | Var s -> s
      | ConstBool b -> b.ToString()
      | ConstInt i -> i.ToString()
      | ConstFloat f -> f.ToString()
      | ConstString s -> sprintf "\"%s\"" s
      | Ref s -> sprintf "ref %s" s
      | Assign (v,c) -> sprintf "%s%s = %s;\n" pre v ((c.AsCSharp "").TrimEnd[|','; '\n'; ';'|])
      | ConstLambda (pc,args,body) ->
        let argss = args |> List.map (fun a -> a + ",")
        sprintf "%s(%s) => %s" pre ((!+argss).TrimEnd[|','|]) (body.AsCSharp (pre + "  "))
      | TypedDef (n,args,t,body) ->
        let argss = args |> List.map (fun (t,a) -> t + " " + a + ",")
        (if t = "" then sprintf "%s%s(%s) {\n%s%s}\n" pre n
         else sprintf "%s%s %s(%s) {\n%s%s}\n" pre t n) ((!+argss).TrimEnd[|','; '\n'|]) (body.AsCSharp (pre + "  ")) pre
      | New(c,args) ->
        let argss = args |> List.map (fun a -> ((a.AsCSharp "").TrimEnd[|','; '\n'; ';'|]) + ",")
        sprintf "%snew %s(%s);\n" pre c ((!+argss).TrimEnd[|','; '\n'; ';'|])
      | Call(n,args) ->
        let argss = args |> List.map (fun a -> ((a.AsCSharp "").TrimEnd[|','; '\n'; ';'|]) + ",")
        sprintf "%s%s(%s);\n" pre n ((!+argss).TrimEnd[|','; '\n'; ';'|])
      | MethodCall(n,m,args) ->
        let argss = args |> List.map (fun a -> ((a.AsCSharp "").TrimEnd[|','; '\n'; ';'|]) + ",")
        sprintf "%s%s.%s(%s);\n" pre n m ((!+argss).TrimEnd[|','; '\n'; ';'|])
      | StaticMethodCall(c,m,args) ->
        let argss = args |> List.map (fun a -> (a.AsPython "").TrimEnd([|'\n'|]) + ",")
        sprintf "%s%s.%s(%s)\n" pre c m ((!+argss).TrimEnd[|','; '\n'; ';'|])
      | If(c,t,e) ->
        sprintf "%sif(%s) {\n%s } else {\n%s }" pre (c.AsCSharp "") (t.AsCSharp (pre + "  ")) (e.AsCSharp (pre + "  "))
      | While(c,b) ->
        sprintf "%swhile(%s) {\n%s }" pre (c.AsCSharp "") (b.AsCSharp (pre + "  "))
      | Op(a,op,b) ->
        sprintf "%s %s %s" ((a.AsCSharp "").Replace("\n","").Replace(";","")) (op.AsCSharp) ((b.AsCSharp (pre + "  ")).Replace("\n","").Replace(";",""))
      | Sequence (p,q) ->
        sprintf "%s%s" (p.AsCSharp pre) (q.AsCSharp pre)
      | _ -> failwith "Unsupported Python statement"
    member this.NumberOfCSharpLines = 
      let code = ((this.AsCSharp ""):string).TrimEnd([|'\n'|])
      let lines = code.Split([|'\n'|])
      lines.Length


let printBindings (b:Map<string, Code>) =
  let pc = b |> Map.tryFind "PC"
  let ret = b |> Map.tryFind "ret"
  let b' = b |> Map.remove "PC" |> Map.remove "ret"
  let entries = [ for x in b' do match x.Value with Hidden _ -> () | _ -> yield x ]
  let innerNames = [ for x in entries do yield x.Key ]
  let innerValues = [ for x in entries do yield x.Value ]
  let names = (match ret with | Option.None -> innerNames | _ -> "ret" :: innerNames)
  let values = (match ret with | Option.None -> innerValues | Some x -> x :: innerValues)
  let names = (match pc with | Option.None -> names | _ -> "PC" :: names)
  let values = (match pc  with | Option.None -> values | Some x -> x :: values)
  let allNames = if names |> List.isEmpty then "" else names |> List.reduce (fun a b -> a + " & " + b)
  let allValues = if values |> List.isEmpty then "" else values |> List.map (fun v -> v.AsPython "") |> List.reduce (fun a b -> a + " & " + b)
  allNames,allValues

type RuntimeState = { Stack : List<Map<string, Code>>; HeapSize : int; Heap : Map<string, Code>; InputStream : List<Code> }
  with 
    member this.AsSlideContent =
      let stackFrames = 
        [
          for sf in this.Stack do
          yield printBindings sf 
        ] |> List.rev
      let stackNamesByFrame = stackFrames |> List.map fst
      let stackValuesByFrame = stackFrames |> List.map snd
      let stackNames = stackNamesByFrame |> List.reduce (fun a b -> a + " & & " + b)
      let stackValues = stackValuesByFrame |> List.reduce (fun a b -> a + " & & " + b)

      let hd = stackNames |> Seq.map (fun _ -> "c") |> Seq.toList
      let stackTableContent = sprintf "%s \\\\\n\\hline\n%s \\\\\n\\hline\n" stackNames stackValues
      let stackTable = sprintf "%s\n%s\n%s\n" (beginTabular hd) stackTableContent endTabular

      let heap = 
        if this.Heap |> Map.isEmpty then
          ""
        else
          let hd = this.Heap |> Seq.map (fun _ -> "c") |> Seq.toList
          let heapNames,heapValues = printBindings this.Heap
          let heapTableContent = sprintf "%s \\\\\n\\hline\n%s \\\\\n\\hline\n" heapNames heapValues
          sprintf "%s\n%s\n%s" (beginTabular hd) heapTableContent endTabular
      stackTable, heap


let lookup (s:RuntimeState) (v:string) =
  let rec lookupHeap (h:Map<string, Code>) vs =
    match vs with
    | [] -> failwith "Empty lookup string"
    | [v] ->
      h.[v]
    | v::vs ->
      match h.[v] with
      | Hidden(Object(bs)) | Object(bs) ->
        lookupHeap bs vs
      | _ -> failwithf "Cannot find %s" v

  let vs = v.Split([|'.'|]) |> Seq.toList
  let y = 
    match s.Stack with
    | [] -> failwith "Cannot find variable in empty stack"
    | c :: rs when c |> Map.containsKey vs.Head -> c.[vs.Head]
    | _ -> (s.Stack |> List.rev |> List.head).[vs.Head]
  match y,vs with
  | _,[v] -> y
  | Ref r,v::vs ->
    match s.Heap.[r] with
    | Object(bs) | Hidden(Object(bs)) ->
      lookupHeap bs vs
    | _ -> failwith "Lookup on non-object value."
  | _ -> failwith "Malformed lookup"

let store (s:RuntimeState) (v:string) (y:Code) : RuntimeState =
  let rec storeHeap (bs:Map<string,Code>) (vs:List<string>) : Map<string,Code> =
    match vs with
    | [] -> bs
    | [v] -> bs |> Map.add v y
    | v::vs ->
      match bs.[v] with
      | Object(bs_inner) | Hidden(Object(bs_inner)) ->
        let k = match bs.[v] with | Object(bs_inner) -> id | _ -> Hidden
        bs |> Map.add v (k(Object(storeHeap bs_inner vs)))
      | _ -> failwith "..."

  let vs = (v.Split([|'.'|]) |> Seq.toList)
  match vs with
  | [v] -> 
    match s.Stack with
    | c :: rs -> { s with Stack = (c |> Map.add v y) :: rs }
    | [] -> failwith "Cannot find variable in empty stack"
  | v::vs ->
    match s.Stack.Head.[v] with
    | Ref r ->
      match s.Heap.[r] with
      | Object(bs) | Hidden(Object(bs)) ->
        let k = match s.Heap.[r] with | Object(bs_inner) -> id | _ -> Hidden
        { s with Heap = s.Heap |> Map.add r (k(Object(storeHeap bs vs))) }
      | _ -> failwith "Malformed assignment"
    | _ -> failwith "Cannot lookup a non-ref object"
  | _ -> failwith "Malformed assignment"



let getPC =
  co{
    let! s = getState
    match s.Stack.Head.["PC"] with
    | ConstInt pc -> return pc
    | _ -> return failwith "Cannot find PC"
  }

let changePC f =
  co{
    let! s = getState
    match s.Stack with
    | c :: rs ->
      match c.["PC"] with
      | ConstInt pc ->
        do! setState { s with Stack = (c |> Map.add "PC" (ConstInt (f pc))) :: rs }
      | _ -> 
        return failwith "Cannot find PC"
    | _ -> 
      return failwith "Cannot find PC"
  }

let incrPC = changePC ((+) 1)

let rec runPython (p:Code) : Coroutine<RuntimeState,Code> =
  co{
    match p with
    | Hidden c -> 
      return! runPython c
    | Ref _ as r ->
      return r
    | Var v -> 
      let! s = getState
      return lookup s v
    | ConstInt i ->
      return ConstInt i
    | ConstFloat f ->
      return ConstFloat f
    | ConstString s ->
      return ConstString s
    | Assign (v,e) ->
      let! res = runPython e
      let! s = getState
      let s_new = store s v res
      do! setState s_new
      return None
    | Return e ->
      let! res = runPython e
      let! s = getState
      match s.Stack with
      | c::rs ->
        do! setState { s with Stack = (Map.empty |> Map.add "PC" c.["PC"] |> Map.add "ret" res) :: rs }
        do! pause
        do! setState { s with Stack = rs }
        return res
      | _ -> return failwith "Cannot return from empty stack"
    | Def(f,args,body) ->
      let! pc = getPC
      let nl = body.NumberOfPythonLines
      do! changePC ((+) nl)
      let! s = getState
      do! setState { s with Heap = (s.Heap |> Map.add f (Hidden(ConstLambda(pc+1,args,body)))) }
      return Assign(f, ConstLambda(pc+1,args,body))
    | Call(f,argExprs) ->
      let! argVals = argExprs |> mapCo runPython
      let! s = getState
      match lookup s f with
      | Hidden(ConstLambda(pc,argNames,body))
      | ConstLambda(pc,argNames,body) ->
        let c = Seq.zip argNames argVals |> Map.ofSeq |> Map.add "PC" (ConstInt pc) |> Map.add "ret" None
        do! setState { s with Stack = c :: s.Stack }
        do! pause
        return! runPython body
      | _ -> return failwithf "Cannot find function %s" f            
    | Op (a,op,b) -> 
      let! aVal = runPython a
      let! bVal = runPython b
      match aVal,bVal with
      | ConstInt x, ConstInt y -> 
        match op with
        | Times -> return ConstInt(x * y)
        | Plus -> return ConstInt(x + y)
        | Minus -> return ConstInt(x - y)
        | DividedBy -> return ConstInt(x / y)
        | GreaterThan -> return ConstBool(x > y)
      | _ -> return failwithf "Cannot perform %s %s %s" (a.AsPython "") op.AsPython (b.AsPython "")
    | If(c,t,e) ->
      let! cVal = runPython c
      match cVal with
      | ConstBool true ->
        return! runPython (Sequence(End,t))
      | ConstBool false ->
        do! changePC ((+) (t.NumberOfPythonLines + 1))
        return! runPython (Sequence(End,e))
      | _ -> return failwith "Malformed if"
    | Sequence (p,k) ->
      let! _ = runPython p
      do! incrPC
      do! pause
      return! runPython k
    | End -> return None
    | ClassDef (n,ms) as cls ->
      let! pc = getPC
      let! s = getState
      let! msVals = ms |> mapCo runPython
      let mutable m_pc = pc + 1
      let msValsByName = 
        [
          for m in msVals do
            match m with
            | Assign(f,ConstLambda(_,args,body)) -> 
              let pc = m_pc + 1
              m_pc <- m_pc + 2 + body.NumberOfPythonLines
              yield f,ConstLambda(pc,args,body)
            | _ -> failwithf "Malformed method definition in class %s" n
        ] |> Map.ofList
      do! setState { s with Heap = (s.Heap |> Map.add n (Hidden(Object(msValsByName |> Map.add "name" (ConstString n))))) }
      let nl = cls.NumberOfPythonLines
      do! changePC ((+) nl)
      return None
    | StaticMethodCall(c,m,argExprs) ->
      let! s = getState
      match s.Heap.[c] with
      | Hidden(Object(ms)) 
      | Object(ms) ->
        let! argVals = argExprs |> mapCo runPython
        let! s = getState
        match ms.[m] with
        | Hidden(ConstLambda(pc,argNames,body))
        | ConstLambda(pc,argNames,body) ->
          let c = Seq.zip argNames argVals |> Map.ofSeq |> Map.add "PC" (ConstInt pc) |> Map.add "ret" None
          do! setState { s with Stack = c :: s.Stack }
          do! pause
          let! res = runPython body
          match res with
          | None -> // automatically returned, pop stack frame here
            let! s = getState
            do! setState { s with Stack = (Map.empty |> Map.add "PC" s.Stack.Head.["PC"] |> Map.add "ret" res) :: s.Stack.Tail }
            do! pause
            do! setState { s with Stack = s.Stack.Tail }
            return res
          | _ -> 
            return res
        | _ -> return failwithf "Cannot call method %s on %s as it is not an object" m c
      | _ -> return failwithf "Cannot find class %s" c
    | MethodCall(v,m,argExprs) ->
      let! s = getState
      match lookup s v with
      | Ref v_ref as v_val ->
        match s.Heap.[v_ref] with
        | Hidden(Object(bs))
        | Object(bs) as o ->
          match bs.["__type"] with
          | Ref(c_name) ->
            match s.Heap.[c_name] with
            | Hidden(Object(ms)) | Object(ms) ->
              match ms.["name"] with
              | ConstString v_type_name ->
                return! runPython (StaticMethodCall(v_type_name, m, v_val :: argExprs))
              | _ -> return failwith ""
            | _ -> return failwith ""
          | _ -> return failwith ""
        | _ -> return failwith ""
      | _ -> return failwith ""
    | New(c,argExprs) ->
      let! s = getState
      match s.Heap.[c] with
      | Hidden(Object(ms))
      | Object(ms) as o ->
        let self = Object (Map.empty |> Map.add "__type" (Ref c))
        let self_ref_id = s.HeapSize.ToString()
        let self_ref = Ref self_ref_id
        do! setState { s with Stack = s.Stack; Heap = s.Heap |> Map.add self_ref_id self; HeapSize = s.HeapSize + 1 }
        do! pause
        let! bodyRes = runPython (StaticMethodCall(c, "__init__", self_ref :: argExprs))
        return self_ref
      | _ -> return failwithf "Cannot find class %s" c
    | c -> return failwithf "Unsupported construct %A" c
//    | TypedVar of string * string
//    | TypedDef of string * List<string * string> * string * Code
  }


let classDef c m = ClassDef(c,m)
let (:=) x y = Assign(x,y)
let newC c a = New(c,a)
let constInt x = ConstInt(x)
let constFloat x = ConstFloat(x)
let constString x = ConstString(x)
let typedDecl x t = TypedDecl(x,t,Option.None)
let typedDeclAndInit x t c = TypedDecl(x,t,Some c)
let var x = Var(x)
let ret x = Return(x)
let def x l b = Def(x,l,b)
let typedDef x l t b = TypedDef(x,l,t,b)
let call x l = Call(x,l)
let methodCall x m l = MethodCall(x,m,l)
let ifelse c t e = If(c,t,e)
let whiledo c b = While(c,b)
let (.+) a b = Op(a, Plus, b)
let (.-) a b = Op(a, Minus, b)
let (.*) a b = Op(a, Times, b)
let (./) a b = Op(a, DividedBy, b)
let (.>) a b = Op(a, GreaterThan, b)
let (>>) a b = Sequence(a, b)
let endProgram = End