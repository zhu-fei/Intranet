﻿module BaseDatos

open System
open MySql
open MySql.Data
open FSharp.Data
open FSharp.Data.Sql
open MySql.Data.MySqlClient
open System.Net
open System.Linq
open System.Collections.Generic
open WebSharper

type Kardex = 
    {Matricula : string
     Grupo  : string
     Materia   : string
     Semestre  : int
     Periodo   : string
     C1        : string option
     I1        : uint32
     C2        : string option
     I2        : uint32
     C3        : string option
     I3        : uint32
     Efinal    : string option
     Final     : string option
     Inasistencias  : int
     Extraordinario : string option
     Regularizacion : string option
     Estatus        : string}

type PrediccionProfesor = {
    materia : string
    grupo   : string
    matricula : string
    nombre    : string
    estatus   : string
    precision : float32
    numero_instancias : int
    atributos : string
    descripcion : string
    descripcion_seleccion : string
    }


type PrediccionAlumno = {
    materia         : string
    grupo           : string
    periodo         : string
    matricula       : string
    nombreAlumno    : string
    nombreProfesor  : string
    apellidos       : string
    c1              : string
    i1              : int
    c2              : string
    i2              : int
    c3              : string
    i4              : int
    estatusPredicho : string
    estatus         : string
    precision       : float32
    numero_instancias : int
    atributos       : string
    descripcion     : string
    descripcion_seleccion : string
    periodo_inicial : string
    periodo_final   : string
    parcial   : int
    }

let db_timeout = 60000

[<Literal>]
let connectionString = @"Server=127.0.0.1; Port=3306; User ID=intranet; Password=intranet; Database=intranet"

[<Literal>]
let resolutionFolder = @"packages/MySql.Data.6.9.9/lib/net45"

[<Literal>]
let dbVendor = Common.DatabaseProviderTypes.MYSQL


type Sql = 
    SqlDataProvider< 
        ConnectionString = connectionString,
        DatabaseVendor = dbVendor,
        ResolutionPath = resolutionFolder,
        UseOptionTypes = true >

let ctx = Sql.GetDataContext()

let to_number number (str : string) =
    try
        number str
    with | :? System.FormatException -> number "0"

let to_number_option number (str : string) =
    try
       Some (number str)
    with | :? System.FormatException -> None

let to_sbyte = to_number sbyte
let to_double = to_number float32
let to_uint32 = to_number uint32

let to_sbyte_option  = to_number_option sbyte
let to_double_option = to_number_option float32
let to_uint32_option = to_number_option uint32

let select_matriculas carrera periodo =
    query { for registro in Sql.GetDataContext().Intranet.Inscripciones do
            where (registro.Plan = carrera && registro.Periodo = periodo)
            select (registro.Matricula)}
            |> Seq.toList

let obtener_clave_profesor (grupo : string) =
    match query {for A in Sql.GetDataContext().Intranet.Grupos do
                 where (A.Grupo = grupo)
                 select A.Profesor}
                |> Seq.toList with
        [profesor] -> profesor
       | _ -> None

let obtener_kardex materia periodo =
    query {for A in Sql.GetDataContext().Intranet.Kardex do
           where (A.Materia = Some materia && A.Periodo = periodo &&
                  A.C1 <> None && A.C2 <> None && A.C3 <> None && A.Efinal <> None && A.Final <> None)
           select (A.Grupo, A.C1, A.I1, A.C2, A.I2, A.C3, A.I3, A.Efinal, A.Final, A.Inasistencias)}
           |> Seq.map (fun (grupo, c1, i1, c2, i2, c3, i3, efinal, final, inasistencias) -> 
                        (Option.get (obtener_clave_profesor grupo), Option.get c1, i1, Option.get c2, i2, Option.get c3, i3, Option.get efinal, Option.get final, inasistencias))

let obtener_claves_profesores (materia : string) (periodo : string) =
    query {for A in Sql.GetDataContext().Intranet.Grupos do
              where (A.Materia = materia && A.Periodo = periodo)
              select A.Profesor}
            |> Seq.choose (fun x -> x)
            |> Seq.toList

let obtener_estatus (materia : string) (periodo : string) =
    query {for A in Sql.GetDataContext().Intranet.Kardex do
              where (A.Materia = Some materia && A.Periodo = periodo)
              select A.Estatus}
            |> Seq.toList

let obtener_datos periodoInicial periodoFinal codigo =
    Sql.GetDataContext().Procedures.DatosEntrenamiento.Invoke(periodoInicial, periodoFinal, codigo).ResultSet
        |> Seq.map (fun r -> r.MapTo<Kardex>())
        |> Seq.distinctBy (fun k -> (k.Matricula, k.Materia))
        |> Seq.toList

[<Rpc>]
let obtener_prediccion_profesor periodo parcial nombre apellidos =
    Sql.GetDataContext().Procedures.GruposProfesor.Invoke(periodo, parcial, nombre, apellidos).ResultSet
        |> Seq.toList
        |> List.map (fun r -> (*r.ColumnValues |> Seq.map fst
                                             |> Seq.iter (printfn "%A")*)
                              r.MapTo<PrediccionProfesor>())
        |> List.map (fun P -> 
                [P.materia
                 P.grupo
                 P.matricula
                 P.nombre
                 P.estatus
                 string P.precision
                 string P.numero_instancias
                 P.atributos
                 P.descripcion
                 P.descripcion_seleccion])
        |> async.Return


let obtener_prediccion_alumno matricula =
    ctx.Procedures.AlumnosProfesor.Invoke(matricula).ResultSet
        |> Seq.toList
        |> List.map (fun r -> r.ColumnValues |> Seq.map (string << snd)
                                             |> Seq.toList)
(*        |> List.map (fun r -> (*r.ColumnValues |> Seq.map fst
                                             |> Seq.iter (printfn "%A")*)
                              r.MapTo<PrediccionAlumno>())
        |> List.map (fun P -> 
                [P.materia
                 P.grupo
                 P.periodo
                 P.matricula
                 P.nombreAlumno
                 P.nombreProfesor
                 P.apellidos
                 P.estatusPredicho
                 P.estatus
                 string P.precision
                 string P.numero_instancias
                 P.atributos
                 P.descripcion
                 P.descripcion_seleccion
                 P.periodo_inicial
                 P.periodo_final
                 string P.parcial])*)
        |> async.Return


[<Rpc>]
let obtener_planes () =
    query {for A in Sql.GetDataContext().Intranet.Planes do
           select A}
           |> Seq.toList
           |> List.map (fun A -> [A.Clave; A.Materia])
           |> async.Return


(*"510F" |> Library.tap (fun _ -> printfn "Calculo pesado empezando...")
       |> obtener_datos "20141S" "20153S"
       |>List.iter (printfn "%A")*)

// matricula nombre genero fecha_nacimiento ingreso telefono direccion colonia cp municipio procedencia
let rec actualiza_alumno (matricula : string) (nombre : string) (genero : string) (fecha_nacimiento : DateTime) ingreso telefono direccion colonia cp municipio procedencia =
    let ctx = Sql.GetDataContext()
    let result = query { for registro in ctx.Intranet.Alumnos do
                         where (registro.Matricula = matricula)
                         select (registro)}
                            |> Seq.toList
    match result with
        [registro] -> registro.Delete()
                      ctx.SubmitUpdates()
                      actualiza_alumno matricula nombre genero fecha_nacimiento ingreso telefono direccion colonia cp municipio procedencia
       | _ -> let registro = ctx.Intranet.Alumnos.Create()
              registro.Matricula <- matricula
              registro.Nombre <- nombre
              registro.Genero <- genero
              registro.FechaNacimiento <- fecha_nacimiento
              registro.Ingreso <- ingreso
              registro.Telefono <- telefono
              registro.Direccion <- direccion
              registro.Colonia <- colonia
              registro.Cp <- cp
              registro.Municipio <- municipio
              registro.Procedencia <- procedencia
              ctx.SubmitUpdates()

let obtener_clave_materia carrera (materia : string) =
    let ctx = Sql.GetDataContext()
    match query {for A in ctx.Intranet.Planes do
                 where (A.Materia = materia && A.Carrera = carrera)
                 select A.Clave}
                |> Seq.toList with
        [clave] -> Some (clave.Trim())
        | [] -> match query {for A in ctx.Intranet.Extracurriculares do
                             where (A.Materia = materia)
                             select A.Clave}
                        |> Seq.toList with
                    [clave] -> Some (clave.Trim())
                  | [] -> printfn "Materia '%s' (%i) no encontrada en la carrera '%s'." materia (String.length materia) carrera
                          None
                  | _ ->  printfn "Más de una materia con nombre %s en la carrera %s" materia carrera
                          None
        | _ -> printfn "Más de una materia con nombre %s en la carrera %s" materia carrera
               None

let rec actualiza_inscripciones (matricula : string) (periodo : string) (estado : string) (semestre : string) (plan : string) (fecha : DateTime) =
    let ctx = Sql.GetDataContext()
    let result = query { for registro in ctx.Intranet.Inscripciones do
                         where (registro.Matricula = matricula && registro.Periodo = periodo)
                         select (registro)}
                            |> Seq.toList
    match result with
        [registro] -> registro.Delete()
                      ctx.SubmitUpdates()
                      actualiza_inscripciones matricula periodo estado semestre plan fecha
       | _ -> let registro = ctx.Intranet.Inscripciones.Create()
              registro.Matricula <- matricula
              registro.Periodo <- periodo
              registro.Estado <- estado
              registro.Semestre <- to_sbyte semestre
              registro.Plan <- plan
              registro.Fecha <- fecha
              ctx.SubmitUpdates()

let rec actualiza_extra (clave : string) (programa : string) (materia : string) (teoria : string) (practica : string) (evaluacion : string) =
    let ctx = Sql.GetDataContext()
    let result = query { for registro in ctx.Intranet.Extracurriculares do
                         where (registro.Clave = clave)
                         select (registro)}
                            |> Seq.toList
    match result with
        [registro] -> registro.Delete()
                      ctx.SubmitUpdates()
                      actualiza_extra clave programa materia teoria practica evaluacion
       | _ -> let registro = ctx.Intranet.Extracurriculares.Create()
              registro.Clave <- clave
              registro.Programa <- programa
              registro.Materia <- materia
              registro.Teoria <- to_sbyte teoria
              registro.Practica <- to_sbyte practica
              registro.Evaluacion <- evaluacion
              ctx.SubmitUpdates()


let rec actualiza_planes carrera clave semestre materia seriacion creditos horas teoria practica evaluacion =
    let ctx = Sql.GetDataContext()
    let result = query { for registro in ctx.Intranet.Planes do
                         where (registro.Carrera = carrera && registro.Clave = clave && registro.Materia = materia)
                         select registro}
                            |> Seq.toList
    match result with
        [registro] -> registro.Delete()
                      ctx.SubmitUpdates()
                      actualiza_planes carrera clave semestre materia seriacion creditos horas teoria practica evaluacion
       | _ -> let registro = ctx.Intranet.Planes.Create()
              registro.Carrera <- carrera
              registro.Clave <- clave
              registro.Semestre <- to_sbyte semestre
              registro.Materia <- materia
              registro.Seriacion <- seriacion
              registro.Creditos <- to_sbyte creditos
              registro.Horas <- to_sbyte horas
              registro.Teoria <- to_sbyte teoria
              registro.Practica <- to_sbyte practica
              registro.Evaluacion <- evaluacion
              ctx.SubmitUpdates()

let rec actualiza_grupos grupo periodo materia aula lunes martes miercoles jueves viernes sabado profesor alumnos estado plan =
    let ctx = Sql.GetDataContext()
    let result = query { for registro in ctx.Intranet.Grupos do
                         where (registro.Grupo = grupo)
                         select registro}
                            |> Seq.toList
    match result with
        [registro] -> registro.Delete()
                      ctx.SubmitUpdates()
                      actualiza_grupos grupo periodo materia aula lunes martes miercoles jueves viernes sabado profesor alumnos estado plan
       | _ -> let registro = ctx.Intranet.Grupos.Create()
              registro.Grupo <- grupo
              registro.Periodo <- periodo
              registro.Materia <- materia
              registro.Aula <- aula
              registro.Lunes <- lunes
              registro.Martes <- martes
              registro.Miercoles <- miercoles
              registro.Jueves <- jueves
              registro.Viernes <- viernes
              registro.Sabado <- sabado
              registro.Profesor <- to_uint32_option profesor
              registro.Alumnos <- to_uint32 alumnos
              registro.Estado <- estado
              registro.Plan <- plan
              ctx.SubmitUpdates()

let rec actualiza_profesores profesor periodo nombre apellidos tipo =
    let ctx = Sql.GetDataContext()
    let result = query { for registro in ctx.Intranet.Profesores do
                         where (registro.Profesor = to_uint32 profesor && registro.Periodo = periodo)
                         select registro}
                            |> Seq.toList
    match result with
        [registro] -> registro.Delete()
                      ctx.SubmitUpdates()
                      actualiza_profesores profesor periodo nombre apellidos tipo
       | _ -> let registro = ctx.Intranet.Profesores.Create()
              registro.Profesor <- to_uint32 profesor
              registro.Periodo <- periodo
              registro.Nombre <- nombre
              registro.Apellidos <- apellidos
              registro.Tipo <- tipo
              ctx.SubmitUpdates()

let rec actualiza_kardex matricula grupo materia semestre periodo c1 i1 c2 i2 c3 i3 efinal final inasistencias extraordinario regularizacion estatus =
    let ctx = Sql.GetDataContext()
    let result = query { for registro in ctx.Intranet.Kardex do
                         where (registro.Matricula = matricula && registro.Grupo = grupo)
                         select (registro)}
                            |> Seq.toList
    match result with
        [registro] -> registro.Delete()
                      ctx.SubmitUpdates()
                      actualiza_kardex matricula grupo materia semestre periodo c1 i1 c2 i2 c3 i3 efinal final inasistencias extraordinario regularizacion estatus
       | _ -> let registro = ctx.Intranet.Kardex.Create()
              registro.Matricula <- matricula
              registro.Grupo <- grupo
              registro.Materia <- materia
              registro.Semestre <- semestre
              registro.Periodo <- periodo
              registro.C1 <- c1
              registro.I1 <- i1
              registro.C2 <- c2
              registro.I2 <- i2
              registro.C3 <- c3
              registro.I3 <- i3
              registro.Efinal <- efinal
              registro.Final <- final
              registro.Inasistencias <- inasistencias
              registro.Extraordinario <- extraordinario
              registro.Regularizacion <- regularizacion
              registro.Estatus <- estatus
              ctx.SubmitUpdates()


let rec actualiza_usuarios usuario contrasena =
    let ctx = Sql.GetDataContext()
    let result = query { for registro in ctx.Intranet.Usuarios do
                         where (registro.Usuario = usuario && registro.Contrasena = contrasena)
                         select registro}
                            |> Seq.toList
    match result with
        [registro] -> registro.Delete()
                      ctx.SubmitUpdates()
                      actualiza_usuarios usuario contrasena
       | _ -> let registro = ctx.Intranet.Usuarios.Create()
              registro.Usuario <- usuario
              registro.Contrasena <- contrasena
              ctx.SubmitUpdates()

