module FSharp.EdIlyin.Http.Response

open Hopac
open FSharp
open FSharp.EdIlyin.Core


let statusCode expecting =
    let label = sprintf "a status code %A" expecting

    Decode.primitive label
        (fun (r:Data.HttpResponse) ->
            let sc = r.StatusCode
            if sc = expecting then Ok sc
            else Decode.expectingButGot label r
        )


let bodyText =
    let label = "text body"

    Decode.primitive label
        (fun (r:Data.HttpResponse) ->
            match r.Body with
                | Data.Binary _ -> Decode.expectingButGot label r
                | Data.Text text -> Ok text
        )


let unpack parser async =
    boxcar {
        let! (response: Data.HttpResponse) =
            Job.fromAsync async |> Boxcar.catch

        let! result =
            try Decode.decode parser response
            with exn -> sprintf "%s\n%A" exn.Message response |> Error
            |> Boxcar.fromResult

        return result
    }


let statusCode200BodyText func =
    Decode.map2 second (statusCode 200) bodyText |> Decode.map func


let statusCode200Json decoder =
    Decode.map2 second (statusCode 200) bodyText
        |> Decode.andThen
            (Json.Decode.decodeString decoder >> Decode.fromResult)
