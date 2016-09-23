namespace Intranet

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI.Next
open WebSharper.UI.Next.Client
open WebSharper.UI.Next.Html

[<JavaScript>]
module Client =
    open WebSharper.Forms
    module B = WebSharper.Forms.Bootstrap.Controls

    let periodos () = 
            "20013S" ::
                [for ano in 2002 .. System.DateTime.Now.Year do
                    for s in 1..3 do
                        yield string ano + string s + "S"]
    let vperiodo period = 
            if not (List.exists (fun s -> s = period) ("*" :: periodos ()))
            then printfn "El valor de periodo debe ser uno de los siguientes valores: %A" periodos
                 true
            else false


    let userComponents username =
        div [
            p [text "Click here to log out:"]
            buttonAttr [
                on.click (fun _ _ ->
                    async {
                        do! Server.LogoutUser username
                        return JS.Window.Location.Reload()
                    } |> Async.Start
                )
            ] [text "Logout"]
        ]

    let adminComponents username =
        div [
            p [text "Actualizar Base de Datos:"]
            buttonAttr [
                on.click (fun _ _ ->
                    async {
                        return JS.Window.Location.Assign "about"
                    } |> Async.Start
                )
            ] [text "Actualizar"]
        ]

    let getCareers (careers : Server.Career) =
            let result = if careers.ITI then ["ITI"] else []
            let result = if careers.ITEM then "ITEM" :: result else result
            let result = if careers.ISTI then "ISTI" :: result else result
            let result = if careers.ITMA then "ITMA" :: result else result
            let result = if careers.LAG then "LAG" :: result else result
            let result = if careers.LMKT then "LMKT" :: result else result
            result


    let UpdatePrograms () =
        let careers : Server.Career = {ITI = false; ITEM = false; ISTI = false; ITMA = false; LAG = false; LMKT = false}
        Form.Return (fun iti item isti itma lag lmkt -> {ITI = iti; ITEM = item; ISTI = isti; ITMA = itma; LAG = lag; LMKT = lmkt} : Server.Career)
        <*> Form.Yield careers.ITI
        <*> Form.Yield careers.ITEM
        <*> Form.Yield careers.ISTI
        <*> Form.Yield careers.ITMA
        <*> Form.Yield careers.LAG
        <*> Form.Yield careers.LMKT
        |> Form.WithSubmit
        |> Form.Run (fun career ->
            async {
                return! Server.UpdatePrograms (getCareers career)
            } |> Async.Start
        )
        |> Form.Render (fun iti item isti itma lag lmkt submit ->
            form [
                h2 [text "Actualizar Programas de Carreras"]
                div [B.Checkbox "ITI"  [] (iti, [], [])
                     B.Checkbox "ITEM" [] (item, [], [])
                     B.Checkbox "ISTI" [] (isti, [], [])]
                div [B.Checkbox "ITMA" [] (itma, [], [])
                     B.Checkbox "LAG"  [] (lag, [], [])
                     B.Checkbox "LMKT" [] (lmkt, [], [])]
                B.Button "Actualizar Programa(s)" [attr.``class`` "btn btn-primary"] submit.Trigger
            ]
        )

    let UpdateGroups () =
        Form.Return (fun period -> period : string)
        <*> (Form.Yield ""
            |> Validation.IsNotEmpty "Necesitas introducir una periodo no nulo, por ejmplo, 20051S o *"
            |> Validation.Is (not << vperiodo) "Necesitas introducir una periodo válido, por ejmplo, 20051S o *")
        |> Form.WithSubmit
        |> Form.Run (fun period ->
            async {
                let periods = if period = "*"
                              then periodos ()
                              else [period]
                return! Server.UpdateGroups periods
            } |> Async.Start
        )
        |> Form.Render (fun period submit ->
            form [
                h2 [text "Actualizar Grupos"]
                B.Simple.InputWithError "Periodo" period submit.View
                B.Button "Actualizar Grupos" [attr.``class`` "btn btn-primary"] submit.Trigger
                B.ShowErrors [attr.style "margin-top:1em"] submit.View
            ]
        )

    let UpdateProfessors () =
        let careers : Server.Career = {ITI = false; ITEM = false; ISTI = false; ITMA = false; LAG = false; LMKT = false}
        Form.Return (fun iti item isti itma lag lmkt period -> (({ITI = iti; ITEM = item; ISTI = isti; ITMA = itma; LAG = lag; LMKT = lmkt} : Server.Career), period))
        <*> Form.Yield careers.ITI
        <*> Form.Yield careers.ITEM
        <*> Form.Yield careers.ISTI
        <*> Form.Yield careers.ITMA
        <*> Form.Yield careers.LAG
        <*> Form.Yield careers.LMKT
        <*> (Form.Yield ""
            |> Validation.IsNotEmpty "Necesitas introducir una periodo no nulo, por ejmplo, 20051S o *"
            |> Validation.Is (not << vperiodo) "Necesitas introducir una periodo válido, por ejmplo, 20051S o *")
        |> Form.WithSubmit
        |> Form.Run (fun (career, period) ->
            async {
                let periods = if period = "*"
                              then periodos ()
                              else [period]
                return! Server.UpdateProfessors (getCareers career) periods
            } |> Async.Start
        )
        |> Form.Render (fun iti item isti itma lag lmkt period submit ->
            form [
                h2 [text "Actualizar Profesores"]
                div [B.Checkbox "ITI"  [] (iti, [], [])
                     B.Checkbox "ITEM" [] (item, [], [])
                     B.Checkbox "ISTI" [] (isti, [], [])]
                div [B.Checkbox "ITMA" [] (itma, [], [])
                     B.Checkbox "LAG"  [] (lag, [], [])
                     B.Checkbox "LMKT" [] (lmkt, [], [])]
                B.Simple.InputWithError "Periodo" period submit.View
                B.Button "Actualizar Profesores" [attr.``class`` "btn btn-primary"] submit.Trigger
            ]
        )

    let LoggedInUser username =
        div [
            p [text "Click here to log out:"]
            buttonAttr [
                on.click (fun _ _ ->
                    async {
                        do! Server.LogoutUser username
                        return JS.Window.Location.Reload()
                    } |> Async.Start
                )
            ] [text "Logout"]
        ]

    let AnonymousUser () =
        Form.Return (fun user pass -> ({User = user; Password = pass} : Server.UserPassword))
        <*> (Form.Yield ""
            |> Validation.IsNotEmpty "Must enter a username")
        <*> (Form.Yield ""
            |> Validation.IsNotEmpty "Must enter a password")
        |> Form.WithSubmit
        |> Form.Run (fun userpass ->
            async {
                do! Server.LoginUser userpass
                return JS.Window.Location.Reload()
            } |> Async.Start
        )
        |> Form.Render (fun user pass submit ->
            form [
                B.Simple.InputWithError "Username" user submit.View
                B.Simple.InputPasswordWithError "Password" pass submit.View
                B.Button "Log in" [attr.``class`` "btn btn-primary"] submit.Trigger
                B.ShowErrors [attr.style "margin-top:1em"] submit.View
            ]
        )

    let Main () =
        let rvInput = Var.Create ""
        let submit = Submitter.CreateOption rvInput.View
        let vReversed =
            submit.View.MapAsync(function
                | None -> async { return "" }
                | Some input -> Server.DoSomething input
            )
        div [
            Doc.Input [] rvInput
            Doc.Button "Send" [] submit.Trigger
            hr []
            h4Attr [attr.``class`` "text-muted"] [text "The server responded:"]
            divAttr [attr.``class`` "jumbotron"] [h1 [textView vReversed]]
        ]
