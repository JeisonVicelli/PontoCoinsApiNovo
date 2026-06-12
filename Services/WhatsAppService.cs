using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace ProjetoPontos.Services;

public class WhatsAppService
{
    private readonly HttpClient _http;
    private readonly string _instanceId;
    private readonly string _token;
    private const string BASE_URL = "https://api.z-api.io/instances";

    public WhatsAppService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _instanceId = config["ZApi:InstanceId"] ?? throw new InvalidOperationException("ZApi:InstanceId não configurado.");
        _token = config["ZApi:Token"] ?? throw new InvalidOperationException("ZApi:Token não configurado.");
        var clientToken = config["ZApi:ClientToken"] ?? throw new InvalidOperationException("ZApi:ClientToken não configurado.");
        _http.DefaultRequestHeaders.Add("Client-Token", clientToken);
    }

    // ── Envio base ──
    public async Task<bool> EnviarMensagemAsync(string telefone, string mensagem)
    {
        try
        {
            var url     = $"{BASE_URL}/{_instanceId}/token/{_token}/send-text";
            var numero  = LimparNumero(telefone);
            if (string.IsNullOrEmpty(numero)) return false;

            var body    = JsonSerializer.Serialize(new { phone = numero, message = mensagem });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(url, content);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    // ── 1. Boas-vindas ao cadastrar ──
    public async Task EnviarBoasVindasAsync(string nome, string telefone)
    {
        var msg =
            $"Olá, {nome}! 👋\n\n" +
            $"Seja bem-vindo(a) ao *PontoCoins* da *Gaia Skate & Surf*! 🛹\n\n" +
            $"A partir de agora, cada compra acumula *pontos* e *cashback* " +
            $"para você usar quando quiser.\n\n" +
            $"Qualquer dúvida é só aparecer no balcão. Conte com a gente! 🤙";

        await EnviarMensagemAsync(telefone, msg);
    }

    // ── 2. Confirmação de venda (mostra só o que foi gerado) ──
    public async Task EnviarAtualizacaoPontosAsync(string nome, string telefone,
        int pontos, decimal cashback, string nivel)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"✅ *Gaia Skate & Surf*");
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

        await EnviarMensagemAsync(telefone, sb.ToString());
    }

    // ── 3. Resgate de pontos por brinde ──
    public async Task EnviarResgatePontosAsync(string nome, string telefone,
        int pontosUsados, int saldoRestante)
    {
        var msg =
            $"🎁 *Gaia Skate & Surf*\n\n" +
            $"Oi, {nome}! Resgate confirmado.\n\n" +
            $"Pontos utilizados: *{pontosUsados} pts*\n" +
            $"Saldo restante: *{saldoRestante} pts*\n\n" +
            $"Até a próxima! 🛹";

        await EnviarMensagemAsync(telefone, msg);
    }

    // ── 4. Alerta: cashback próximo do vencimento ──
    public async Task EnviarAlertaExpiracaoAsync(string nome, string telefone,
        decimal valorExpirando, int diasRestantes)
    {
        var prazo = diasRestantes == 1 ? "amanhã" : $"em *{diasRestantes} dias*";
        var msg =
            $"⚠️ *Gaia Skate & Surf*\n\n" +
            $"Atenção, {nome}!\n\n" +
            $"Você tem *R$ {valorExpirando:F2}* em cashback que vence {prazo}.\n\n" +
            $"Passe na loja e use antes de expirar! 🛹";

        await EnviarMensagemAsync(telefone, msg);
    }

    // ── 5. Alerta: pontos próximos do vencimento ──
    public async Task EnviarAlertaPontosAsync(string nome, string telefone,
        int pontos, int diasRestantes)
    {
        var prazo = diasRestantes == 1 ? "amanhã" : $"em *{diasRestantes} dias*";
        var msg =
            $"⚠️ *Gaia Skate & Surf*\n\n" +
            $"Atenção, {nome}!\n\n" +
            $"Seus *{pontos} pontos* vencem {prazo}.\n\n" +
            $"Venha trocar por brindes antes que expirem! 🛹🎁";

        await EnviarMensagemAsync(telefone, msg);
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
