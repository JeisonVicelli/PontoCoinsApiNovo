using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjetoPontos.Models
{
    [Table("Pedido")]
    public class Pedido
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        [ForeignKey("ClienteId")]
        public Cliente Cliente { get; set; }

        [Column("ValorTotalPontos")]
        public int ValorTotalPontos { get; set; }

        [NotMapped]
        public List<Brinde> Brindes { get; set; }

        public Pedido()
        {
            Brindes = new List<Brinde>();
        }

        public void AdicionarBrinde(Brinde brinde)
        {
            Brindes.Add(brinde);
        }
        
        public int CalcularPontos()
        {
            int pontos = 0;

            if(Brindes != null)
            {
              foreach (Brinde brinde in Brindes)
              {
                  pontos += (int)brinde.ValorPontos;
              }
            }
            return pontos;
            }
            }
        }
  

