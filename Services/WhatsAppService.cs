using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProjetoPontos.Models;

namespace ProjetoPontos.Services;

public class WhatsAppService
{
    private readonly HttpClient _http;
    private readonly ILogger<WhatsAppService> _logger;
    private const string BASE_URL = "https://api.z-api.io/instances";

    public WhatsAppService(HttpClient http, ILogger<WhatsAppService> logger)
    {
        _http = http;
        _logger = logger;
    }

    // ── Envio base ──
    public async Task<bool> EnviarMensagemAsync(Loja loja, string telefone, string mensagem)
    {
        if (string.IsNullOrWhiteSpace(loja.ZApiInstanceId) ||
            string.IsNullOrWhiteSpace(loja.ZApiToken) ||
            string.IsNullOrWhiteSpace(loja.ZApiClientToken))
        {
            _logger.LogWarning("Loja {LojaId} ({Nome}) sem credenciais Z-API configuradas — envio de WhatsApp ignorado.", loja.Id, loja.Nome);
            return false;
        }

        try
        {
            var url    = $"{BASE_URL}/{loja.ZApiInstanceId}/token/{loja.ZApiToken}/send-text";
            var numero = LimparNumero(telefone);
            if (string.IsNullOrEmpty(numero)) return false;

            var body = JsonSerializer.Serialize(new { phone = numero, message = mensagem });
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Client-Token", loja.ZApiClientToken);

            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao enviar mensagem WhatsApp para a loja {LojaId}.", loja.Id);
            return false;
        }
    }

    // ── 1. Boas-vindas ao cadastrar (com senha temporária) ──
    public async Task<bool> EnviarBoasVindasAsync(Loja loja, Cliente cliente, string senhaTemporaria)
    {
        var nome = cliente.Nome ?? "Cliente";
        var msg =
            $"E aí, {nome}! 🛹 Seu cadastro no PontoCoins da {loja.Nome} tá pronto — bem-vindo ao squad!\n\n" +
            $"Sua senha temporária de acesso é: *{senhaTemporaria}*\n\n" +
            $"Acessa e troca pra uma senha sua assim que der, é rapidinho. Bons rolês e até a próxima!";

        return await EnviarMensagemAsync(loja, cliente.NumeroTelefone ?? "", msg);
    }

    // ── 2. Confirmação de venda (mostra só o que foi gerado) ──
    public async Task EnviarAtualizacaoPontosAsync(Loja loja, string nome, string telefone,
        int pontos, decimal cashback, string nivel)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"✅ *{loja.Nome}*");
        sb.AppendLine();
        sb.AppendLine($"Oi, {nome}! Sua compra foi registrada. 🛹");
        sb.AppendLine();
        sb.AppendLine("Seu saldo atual:");

        if (pontos > 0)
            sb.AppendLine($"⭐ Pontos: *{pontos} pts*");

        if (cashback > 0)
            sb.AppendLine($"💰 Cashback: *R$ {cashback:F2}*");

        sb.AppendLine($"🏆 Nível: *{nivel}*");
        sb.AppendLine();
        sb.Append("Obrigado pela preferência!");

        await EnviarMensagemAsync(loja, telefone, sb.ToString());
    }

    // ── 3. Resgate de pontos por brinde ──
    public async Task EnviarResgatePontosAsync(Loja loja, string nome, string telefone,
        int pontosUsados, int saldoRestante)
    {
        var msg =
            $"🎁 *{loja.Nome}*\n\n" +
            $"Oi, {nome}! Resgate confirmado.\n\n" +
            $"Pontos utilizados: *{pontosUsados} pts*\n" +
            $"Saldo restante: *{saldoRestante} pts*\n\n" +
            $"Até a próxima! 🛹";

        await EnviarMensagemAsync(loja, telefone, msg);
    }

    // ── 4. Alerta: cashback próximo do vencimento ──
    public async Task EnviarAlertaExpiracaoAsync(Loja loja, string nome, string telefone,
        decimal valorExpirando, int diasRestantes)
    {
        var prazo = diasRestantes == 1 ? "amanhã" : $"em *{diasRestantes} dias*";
        var msg =
            $"🛹 *{loja.Nome}*\n\n" +
            $"E aí, {nome}!\n\n" +
            $"Você tem *R$ {valorExpirando:F2}* em cashback que vence {prazo}.\n\n" +
            $"Passe na loja e use antes de expirar! 🛹";

        await EnviarMensagemAsync(loja, telefone, msg);
    }

    // ── 5. Alerta: pontos próximos do vencimento ──
    public async Task EnviarAlertaPontosAsync(Loja loja, string nome, string telefone,
        int pontos, int diasRestantes)
    {
        var prazo = diasRestantes == 1 ? "amanhã" : $"em *{diasRestantes} dias*";
        var msg =
            $"🛹 *{loja.Nome}*\n\n" +
            $"E aí, {nome}!\n\n" +
            $"Seus *{pontos} pontos* vencem {prazo}.\n\n" +
            $"Venha trocar por brindes antes que expirem! 🛹🎁";

        await EnviarMensagemAsync(loja, telefone, msg);
    }

    // ── Formata número para o padrão internacional brasileiro ──
    private static string LimparNumero(string telefone)
    {
        if (string.IsNullOrWhiteSpace(telefone)) return "";
        var digits = new string(telefone.Where(char.IsDigit).ToArray());

        // 11 dígitos = DDD (2) + 9 + número (8) → adiciona código do Brasil
        if (digits.Length == 11) return "55" + digits;
        // 10 dígitos = DDD (2) + número (8) → adiciona código do Brasil
        if (digits.Length == 10) return "55" + digits;
        // já tem código do país (13 dígitos)
        if (digits.Length == 13) return digits;

        return digits;
    }
}
