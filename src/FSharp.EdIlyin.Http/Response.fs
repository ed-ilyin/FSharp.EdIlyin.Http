module FSharp.EdIlyin.Http.Response

open Hopac
open FSharp
open FSharp.EdIlyin.Core


let statusCode expecting =
    let label = sprintf "a status code %A" expecting

    Decode.primitive label
        (fun (r:Data.HttpResponse) ->
            let sc = r.StatusCode
            if sc = expecting then Decode.decoded sc
            else Decode.expectingButGot label r
        )


let bodyText =
    let label = "text body"

    Decode.primitive label
        (fun (r:Data.HttpResponse) ->
            match r.Body with
                | Data.Binary _ -> Decode.expectingButGot label r
                | Data.Text text -> Decode.decoded text
        )


let unpack parser async =
    boxcar {
        let! (response: Data.HttpResponse) =
            Job.fromAsync async |> Boxcar.catch

        let! result =
            try
                do printfn "Response %i from %s"
                    response.StatusCode
                    response.ResponseUrl

                Decode.decode parser response

            with | exn ->
                sprintf "%s\n%A" exn.Message response |> Error
            |> Boxcar.fromResult

        return result
    }


let statusCode200BodyText decoder =
    Decode.map2 second (statusCode 200) bodyText
        |> Decode.andThen
            (Json.Decode.decodeString decoder >> Decode.fromResult)
