function inicializarFormulariotransacciones(urlObtenerCategorias) {

    /*Cuando una etiqueta con el ID TipoOperacionId (O sea, los marcados con el asp-for=TipoOperacionId
                cambian (.change), entonces se captura el valor que se seleccionó. (valorSeleccionado = $(this).val();)*/

    $("#TipoOperacionId").change(async function () {
        const valorSeleccionado = $(this).val();

        /* Entonces se crea un await que espera un FetchApi que le envía un POST a la acción ObtenerCategorias,
            ubicado en el CONTROLADOR TransaccionesController con el dato del valor seleccionado en el cuerpo del json
            
            
           Fetch recibe la url a la que debe enviar el POST, asi como una estructura de un json. En los parámetros
           que recibe la acción ObtenerCategorias deberia haber un atributo [FromBody] que lo que va a hacer es leer
           el body del json, en este caso el valor seleccionado, y luego lo convierte al tipo de dato que se espera.*/

        const respuesta = await fetch(urlObtenerCategorias, {
            method: 'POST',
            body: valorSeleccionado,
            headers: {
                'Content-Type': 'application/json'
            }
        });

        /* Lo que fue definido como respuesta va a resultar siendo la respuesta al json enviado por el fetch. En este caso,
            lo que va a devolver es un IEnumerable<SelectListItem> con el listado de elementos de las categorias filtrado por
            el valorSeleccionado, que en este caso es lo que se seleccione por el select que tiene como ID TipoOperacionId.
            
           Una vez obtiene ese valor de respuesta, lo guarda en una variable de nombre json, y procede entonces a mapearlo,
           (.map) donde cada categoria de ese IEnumerable que se recibió la va a transformar en una etiqueta <option> con el
           atributo value como categoria.value y cada cuerpo de la categoria como categoria.text. La variable opciones va a
           quedar como un arreglo de esas etiquetas de <option>.
           
           Finalmente, en la parte de $("#CategoriaId").html(opciones) se insertan las opciones en el elemento del html
           que tenga el ID CategoriaId*/

        const json = await respuesta.json();
        const opciones = json.map(categoria => `<option value = ${categoria.value}>${categoria.text}</option>`);

        $("#CategoriaId").html(opciones);
    })
}