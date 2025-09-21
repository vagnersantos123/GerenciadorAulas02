using System;
using System.Collections.Generic;
using System.Linq;

namespace GerenciadorAulas02.Helpers
{
    public static class FilterHelper
    {
        /// <summary>
        /// Filtra uma coleção genérica usando várias propriedades como critério de busca.
        /// </summary>
        /// <typeparam name="T">Tipo dos itens</typeparam>
        /// <param name="items">Lista de itens original</param>
        /// <param name="searchText">Texto de pesquisa</param>
        /// <param name="selectors">Propriedades onde a busca será aplicada</param>
        /// <returns>Lista filtrada</returns>
        public static IEnumerable<T> Filter<T>(
            IEnumerable<T> items,
            string searchText,
            params Func<T, string>[] selectors)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return items;

            searchText = searchText.ToLower();

            return items.Where(item =>
                selectors.Any(selector =>
                {
                    var value = selector(item);
                    return !string.IsNullOrEmpty(value) &&
                           value.ToLower().Contains(searchText);
                }));
        }
    }
}
