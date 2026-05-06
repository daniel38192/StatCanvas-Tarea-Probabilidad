# Plan de implementacion: Distribucion T-Student

## Objetivo

Agregar un nuevo modulo ASP.NET MVC para calcular y visualizar una prueba t de una muestra usando una coleccion de datos observados ingresada por el usuario.

El modulo debe:

- Recibir una lista de observaciones numericas.
- Calcular estadisticos base de la muestra.
- Calcular el estadistico t.
- Mostrar `df`, valor critico, `p-value`, intervalo de confianza y conclusion.
- Renderizar una grafica tipo campana para la distribucion t.

## Punto funcional importante

Con solo la lista de datos no es posible obtener todos los resultados mostrados en el ejemplo.

Para calcular:

- `Estadistico t`
- `p-value`
- `Valor critico`
- `Conclusion`

tambien se necesita definir:

- `H0`: hipotesis nula, expresada con `mu0`.
- `H1`: hipotesis alternativa.
- `alpha`: nivel de significancia.
- tipo de prueba: bilateral, cola izquierda o cola derecha.

Para que el resultado coincida con el ejemplo mostrado, el formulario debe pedir al menos:

- datos observados.
- valor de `mu0` para `H0`.
- definicion de `H1`.
- nivel de significancia `alpha`.

## Estructura propuesta

Seguir el patron ya usado por el proyecto en modulos como `StandardNormal`:

- `Controllers/TStudent/TStudentController.cs`
- `Models/TStudent/TStudentInputViewModel.cs`
- `Models/TStudent/TStudentResultViewModel.cs`
- `Views/TStudent/Index.cshtml`
- `Views/TStudent/Result.cshtml`

Tambien se deben actualizar:

- `Views/Shared/_Layout.cshtml`
- `Views/Home/Index.cshtml`

## Alcance tecnico

### 1. Entrada de datos

El formulario `Index` debe permitir:

- ingresar los datos observados en un `textarea` o campo de texto separado por comas.
- ingresar `mu0`.
- mostrar las hipotesis:
  - `H0: mu = mu0`
  - `H1: mu != mu0`, `mu < mu0` o `mu > mu0`
- seleccionar `alpha` desde un `<select>`.
- seleccionar el tipo de prueba:
  - bilateral
  - izquierda
  - derecha

Valores iniciales del `<select>`:

- `0.05`
- `0.10`
- `0.15`

Validaciones:

- no permitir lista vacia.
- no permitir valores no numericos.
- exigir al menos 2 observaciones.
- validar que `alpha` este entre 0 y 1.

### 2. ViewModels

`TStudentInputViewModel` debe contener al menos:

- `string RawValues`
- `double? HypothesizedMean`
- `double? Alpha`
- `string TestType`
- `string NullHypothesisText`
- `string AlternativeHypothesisText`

`TStudentResultViewModel` debe contener al menos:

- `IReadOnlyList<double> Values`
- `int SampleSize`
- `double SampleMean`
- `double SampleStandardDeviation`
- `int DegreesOfFreedom`
- `double HypothesizedMean`
- `double Alpha`
- `string TestType`
- `string NullHypothesisText`
- `string AlternativeHypothesisText`
- `double TStatistic`
- `double CriticalValue`
- `double PValue`
- `double ConfidenceIntervalLower`
- `double ConfidenceIntervalUpper`
- `string Conclusion`
- `string Svg`

### 3. Logica de calculo

El controlador debe:

1. parsear la cadena de entrada a `double[]`.
2. calcular `n`.
3. calcular media muestral.
4. calcular desviacion estandar muestral `s`.
5. calcular `df = n - 1`.
6. calcular el estadistico:

`t = (x̄ - mu0) / (s / sqrt(n))`

7. calcular valor critico segun `alpha`, `df` y tipo de prueba.
8. calcular `p-value`.
9. calcular intervalo de confianza de la media.
10. construir la conclusion en texto usando `H0`, `H1`, `p-value` y `alpha`.

### 4. Distribucion t y funciones numericas

El proyecto hoy no tiene una libreria estadistica externa, asi que hay dos opciones:

#### Opcion recomendada

Agregar `MathNet.Numerics` para usar funciones confiables de:

- CDF de t-Student
- inversa de la CDF para valor critico

Ventajas:

- menos riesgo numerico.
- implementacion mas corta.
- resultados mas consistentes.

#### Opcion sin dependencia externa

Implementar internamente:

- PDF de t-Student
- CDF aproximada
- inversa numerica para percentiles

Esta opcion implica mas trabajo y mas riesgo de errores numericos.

### 5. Grafica

Reutilizar el enfoque de `StandardNormalController`, que ya genera SVG manualmente.

La grafica debe mostrar:

- curva de la distribucion t con `df` calculados.
- linea vertical en el valor `t`.
- zona sombreada del `p-value` segun el tipo de prueba.
- opcion de abrir el SVG igual que en el modulo de normal estandar.

Implementacion sugerida:

- crear un metodo `BuildSvg(...)`.
- muestrear puntos en un rango dinamico, por ejemplo `[-4.5, 4.5]` o ajustado al `t` y al valor critico.
- usar una funcion `StudentTPdf(x, df)`.

### 6. Presentacion del resultado

La vista `Result` debe mostrar:

- `H0`
- `H1`
- `n`
- media muestral
- desviacion estandar
- `df`
- estadistico `t`
- valor critico
- `p-value`
- intervalo de confianza
- conclusion
- grafica SVG

Formato esperado de salida:

- `Estadistico t = -0.33`
- `df = 9`
- `Valor critico = +/-2.262`
- `p-value ~= 0.75`
- `Intervalo de confianza ~= [29.1, 30.5]`
- `Conclusion: No se rechaza H0`

## Cambios concretos por archivo

### Nuevos archivos

- `Controllers/TStudent/TStudentController.cs`
- `Models/TStudent/TStudentInputViewModel.cs`
- `Models/TStudent/TStudentResultViewModel.cs`
- `Views/TStudent/Index.cshtml`
- `Views/TStudent/Result.cshtml`

### Archivos existentes a modificar

- `Views/Shared/_Layout.cshtml`
  - agregar enlace de navegacion al nuevo modulo.
- `Views/Home/Index.cshtml`
  - agregar tarjeta o acceso rapido a T-Student.
- `ProyectoDeProbabilidadYEstadistica.csproj`
  - solo si se agrega `MathNet.Numerics`.

## Secuencia de implementacion

1. Crear los ViewModels de entrada y resultado.
2. Crear el controlador con el flujo `Index`, `Calculate` y opcionalmente `Svg`.
3. Implementar parseo y validaciones.
4. Implementar calculos muestrales base.
5. Mapear `H1` al tipo de prueba seleccionado.
6. Resolver calculo de CDF/inversa de t-Student.
7. Construir la conclusion segun `p-value` y `alpha`.
8. Crear la vista `Index`.
9. Crear la vista `Result`.
10. Implementar la grafica SVG.
11. Agregar navegacion en layout e inicio.
12. Probar con casos conocidos.

## Casos de prueba sugeridos

### Caso 1

Datos:

- `28, 32, 31, 29, 27, 30`

Validar:

- parseo correcto.
- `n = 6`
- `df = 5`

### Caso 2

Usar un conjunto de 10 datos para verificar un caso con:

- `n = 10`
- `df = 9`
- estadistico cercano al ejemplo

### Caso 3

Entradas invalidas:

- texto no numerico
- un solo dato
- lista vacia
- `alpha` fuera de rango
- `alpha` distinto de `0.05`, `0.10` o `0.15`

## Riesgos y decisiones pendientes

- Definir si el modulo sera solo para prueba t de una muestra.
- Definir si `mu0` sera obligatorio o si habra un valor por defecto.
- Decidir si se permitira instalar `MathNet.Numerics`.
- Definir si la conclusion usara la regla por `p-value` o comparacion directa con valor critico, idealmente ambas de forma consistente.

## Recomendacion

Implementar la primera version como prueba t de una muestra con:

- `mu0` obligatorio.
- `H0: mu = mu0`.
- `H1` elegible entre bilateral, izquierda o derecha.
- `alpha` en un `<select>` con `0.05`, `0.10` y `0.15`.
- `0.05` por defecto.
- grafica SVG reutilizando la base del modulo `StandardNormal`.
- `MathNet.Numerics` para CDF e inversa.

Eso mantiene el alcance controlado y encaja bien con la arquitectura actual del proyecto.
