using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using ProjetoPontos.Data;

namespace ProjetoPontos.Filters;

// Bloqueia o acesso de um Cliente autenticado a endpoints de autoatendimento enquanto
// PrecisaTrocarSenha == true (senha temporária do cadastro inline ainda não foi trocada).
public class ExigeSenhaDefinidaAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var cpf = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(cpf))
        {
            var dbContext = context.HttpContext.RequestServices.GetRequiredService<LojaDbContext>();
            var cliente = await dbContext.Clientes!.FindAsync(cpf);

            if (cliente is not null && cliente.PrecisaTrocarSenha)
            {
                context.Result = new ObjectResult(new
                {
                    mensagem = "Você está usando uma senha temporária. Troque sua senha em POST /Cliente/trocar-senha antes de continuar."
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }
        }

        await next();
    }
}
