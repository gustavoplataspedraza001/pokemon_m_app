using app_poke.Models;
using app_poke.Models.Pokemon;
using app_poke.Service;
using ClosedXML.Excel;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Net.Mail;
using System.Net.Mime;
using ContentType = MimeKit.ContentType;

namespace app_poke.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly PokemonApiService _pokemonService;
        private const int PageSize = 20;

        public HomeController(PokemonApiService pokemonService)
        {
            _pokemonService = pokemonService;
        }

        public async Task<IActionResult> Index(string typeFilter, string searchName, int page = 1)
        {
            // Obtener todos los tipos para combo
            var allTypes = await _pokemonService.GetAllTypesAsync();

            // Obtener pokemones (paginar aquí)
            int offset = (page - 1) * PageSize;
            var pokemons = await _pokemonService.GetPokemonsAsync(offset, PageSize * 5); // Traigo más para filtrar

            // Filtrar por tipo
            if (!string.IsNullOrEmpty(typeFilter))
            {
                pokemons = pokemons.Where(p => p.Types.Contains(typeFilter)).ToList();
            }

            // Filtrar por nombre
            if (!string.IsNullOrEmpty(searchName))
            {
                pokemons = pokemons.Where(p => p.Name.Contains(searchName, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Paginación de la lista filtrada
            int totalItems = pokemons.Count;
            int totalPages = (int)Math.Ceiling(totalItems / (double)PageSize);
            var pokemonsPaged = pokemons.Skip((page - 1) * PageSize).Take(PageSize).ToList();

            var model = new PokemonListViewModel
            {
                Pokemons = pokemonsPaged,
                CurrentPage = page,
                TotalPages = totalPages,
                PokemonTypes = allTypes,
                SelectedType = typeFilter,
                SearchName = searchName
            };

            return View(model);
        }
        public async Task<IActionResult> ExportExcel()
        {
            try
            {
                int limit = 1000;

                using var client = new HttpClient();
                var response = await client.GetStringAsync($"https://pokeapi.co/api/v2/pokemon?offset=0&limit={limit}");

                if (string.IsNullOrWhiteSpace(response))
                    throw new Exception("Respuesta vacía o nula de la API.");

                var data = JObject.Parse(response);
                var results = data["results"];

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Pokemons");

                worksheet.Cell(1, 1).Value = "ID";
                worksheet.Cell(1, 2).Value = "Nombre";
                worksheet.Cell(1, 3).Value = "Imagen URL";

                int row = 2;
                int id = 1;

                foreach (var item in results)
                {
                    worksheet.Cell(row, 1).Value = id;
                    worksheet.Cell(row, 2).Value = item["name"]?.ToString() ?? "Sin nombre";
                    worksheet.Cell(row, 3).Value = $"https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/{id}.png";
                    row++;
                    id++;
                }

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                string fileName = $"Pokemons_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                // Log real
                _logger.LogError(ex, "Error inesperado al exportar Excel");

                // Devuelve JSON de error si la llamada fue con fetch o AJAX
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = ex.Message });
                }

                TempData["ErrorMessage"] = $"Error al generar el archivo: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> SendEmail()
        {
            try
            {
                // 1. Obtener los datos desde la API como en el Excel
                using var client = new HttpClient();
                int limit = 1000;
                var response = await client.GetStringAsync($"https://pokeapi.co/api/v2/pokemon?offset=0&limit={limit}");

                if (string.IsNullOrWhiteSpace(response))
                    throw new Exception("La respuesta de la API está vacía o nula.");

                var data = JObject.Parse(response);
                var results = data["results"];

                // 2. Generar archivo Excel
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Pokemons");

                worksheet.Cell(1, 1).Value = "ID";
                worksheet.Cell(1, 2).Value = "Nombre";
                worksheet.Cell(1, 3).Value = "Imagen URL";

                int row = 2;
                int id = 1;

                foreach (var item in results)
                {
                    worksheet.Cell(row, 1).Value = id;
                    worksheet.Cell(row, 2).Value = item["name"]?.ToString();
                    worksheet.Cell(row, 3).Value = $"https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/{id}.png";
                    row++;
                    id++;
                }

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                // 3. Crear el mensaje MIME
                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse("TU_CORREO@gmail.com")); // <-- CAMBIA ESTO
                email.To.Add(MailboxAddress.Parse("DESTINATARIO@gmail.com")); // <-- CAMBIA ESTO
                email.Subject = "Listado de Pokémon";

                var builder = new BodyBuilder
                {
                    TextBody = "Hola, se adjunta el archivo Excel con el listado de Pokémon.",
                };

                // Adjuntar el Excel
                builder.Attachments.Add($"Pokemons_{DateTime.Now:yyyyMMddHHmmss}.xlsx", stream.ToArray(),
                    new ContentType("application", "vnd.openxmlformats-officedocument.spreadsheetml.sheet"));

                email.Body = builder.ToMessageBody();

                // 4. Enviar con SMTP
                using var smtp = new MailKit.Net.Smtp.SmtpClient();
                await smtp.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls); // <-- CAMBIA SI USAS OTRO SERVIDOR
                await smtp.AuthenticateAsync("", ""); // <-- CAMBIA
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                TempData["SuccessMessage"] = "Correo enviado correctamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar el correo.");
                TempData["ErrorMessage"] = "Hubo un problema al enviar el correo.";
                return RedirectToAction("Index");
            }
        }

    }
}
