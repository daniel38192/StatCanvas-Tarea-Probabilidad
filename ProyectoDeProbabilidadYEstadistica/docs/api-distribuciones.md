# API de Distribuciones

Este cambio agrega servicios singleton para centralizar la logica estadistica de distribucion normal estandar y t de Student, y encima de ellos expone endpoints JSON para calcular resultados sin depender de las vistas Razor.

## Objetivo

- Reutilizar la logica ya existente de los modulos `StandardNormal` y `TStudent`.
- Permitir que la futura pagina de coleccion de problemas consulte resultados via HTTP.
- Mantener una sola fuente de verdad para validacion, calculo y generacion de resultados.

## Servicios agregados

Se registran como singleton en la aplicacion:

- `IStandardNormalService` -> `StandardNormalService`
- `ITStudentService` -> `TStudentService`

Estos servicios concentran:

- validacion de entradas
- calculo estadistico
- generacion de resultados
- generacion de SVG para las vistas MVC

## Endpoints

### `POST /api/standard-normal/calculate`

Calcula area a partir de `z` o calcula `z` a partir de un area.

#### Request JSON

```json
{
  "calculationMode": "from-z",
  "zValue": 1.25,
  "areaValue": null,
  "areaSide": "left"
}
```

#### Campos

- `calculationMode`: `"from-z"` o `"from-area"`.
- `zValue`: requerido cuando `calculationMode = "from-z"`.
- `areaValue`: requerido cuando `calculationMode = "from-area"`. Debe estar entre `0` y `1`, sin incluir extremos.
- `areaSide`: `"left"` o `"right"`.

#### Response 200

```json
{
  "calculationMode": "from-z",
  "zValue": 1.25,
  "areaSide": "left",
  "area": 0.89435,
  "complementArea": 0.10565
}
```

#### Ejemplo para calcular `z` desde area

```json
{
  "calculationMode": "from-area",
  "areaValue": 0.9,
  "areaSide": "left"
}
```

### `POST /api/t-student/calculate`

Calcula estadistico `t`, valor critico, `p-value`, intervalo de confianza y conclusion.

#### Request JSON

```json
{
  "values": [42.1, 40.5, 44.2, 43.7, 41.9],
  "hypothesizedMean": 45.0,
  "alpha": 0.05,
  "testType": "two-tailed"
}
```

#### Campos

- `values`: arreglo de numeros. Debe incluir al menos 2 observaciones y tener variacion.
- `hypothesizedMean`: valor de `mu0`.
- `alpha`: nivel de significancia entre `0` y `1`.
- `testType`: `"two-tailed"`, `"left-tailed"` o `"right-tailed"`.

#### Response 200

```json
{
  "values": [42.1, 40.5, 44.2, 43.7, 41.9],
  "sampleSize": 5,
  "sampleMean": 42.48,
  "sampleStandardDeviation": 1.525123,
  "degreesOfFreedom": 4,
  "hypothesizedMean": 45.0,
  "alpha": 0.05,
  "testType": "two-tailed",
  "nullHypothesisText": "H0: mu = 45",
  "alternativeHypothesisText": "H1: mu != 45",
  "tStatistic": -3.694,
  "criticalValue": 2.776,
  "pValue": 0.021,
  "confidenceIntervalLower": 40.586,
  "confidenceIntervalUpper": 44.374,
  "conclusion": "Se rechaza H0."
}
```

## Respuestas de error

Si la entrada es invalida, los endpoints regresan `400 Bad Request` con `ValidationProblemDetails`.

Ejemplo:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "AreaValue": [
      "El area debe ser mayor que 0 y menor que 1."
    ]
  }
}
```

## Integracion con `docs/problemas.md`

Los endpoints cubren los calculos base necesarios para la coleccion de problemas:

- normal estandar: probabilidades y percentiles
- t de Student: pruebas de hipotesis e intervalos de confianza

Con esto ya se puede construir una pagina que tome un problema del archivo `docs/problemas.md`, arme el request JSON adecuado y renderice la respuesta.

## Archivos involucrados

- `Program.cs`
- `Controllers/StandardNormal/StandardNormalController.cs`
- `Controllers/TStudent/TStudentController.cs`
- `Controllers/Api/StandardNormalApiController.cs`
- `Controllers/Api/TStudentApiController.cs`
- `Services/StandardNormal/*`
- `Services/TStudent/*`
- `Models/Statistics/*`
- `Models/Api/*`

## Notas de diseno

- Los servicios son singleton porque no guardan estado mutable por request.
- Los controladores MVC quedaron como adaptadores de vista.
- Los controladores API consumen y producen JSON.
- La logica de calculo ya no depende de Razor ni de `ModelState`.
