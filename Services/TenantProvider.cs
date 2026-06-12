using Microsoft.AspNetCore.Http;

namespace ProjetoPontos.Services;

public interface ITenantProvider
{
    int LojaId { get; }
}

public class TenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int LojaId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("LojaId")?.Value;
            if (!int.TryParse(claim, out var lojaId))
            {
                throw new InvalidOperationException("Claim 'LojaId' ausente ou inválido no token do usuário autenticado.");
            }

            return lojaId;
        }
    }
}

// Usado fora de requisições HTTP (ex.: jobs em background), onde a LojaId
// é conhecida de antemão e não vem de um claim do HttpContext.
public class StaticTenantProvider : ITenantProvider
{
    public int LojaId { get; }

    public StaticTenantProvider(int lojaId)
    {
        LojaId = lojaId;
    }
}
