﻿using MySql.Data.MySqlClient;
using Mysqlx;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;
using System.Windows.Forms;
using System.Xml;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Bar_do_Esas
{
    public partial class FormularioBar : Form
    {    
        decimal totalAcumulado = 0; //variable that stores the balances        
        decimal somarValorFaltante = 0;  //Sum the value in your balance when you remove a item
        public int N_Funcionario;

        int[] idComida = new int[1];

        public FormularioBar()
        {
            InitializeComponent();

            GerirAcoesLstComida.CriarColunasLstComida(lstBar);

            LoginFuncionario f_login = new LoginFuncionario(this, N_Funcionario);
            f_login.ShowDialog();

            preencherCombo();
        }
     
        #region Buttons
        private void btnAluno_Click(object sender, EventArgs e)
        {
            FormularioAluno f_aluno = new FormularioAluno();
            ChecarLogin(f_aluno);
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            GerirAcoesLstBar checarSaldo = new GerirAcoesLstBar();

            if (cbItem.SelectedItem != null && !string.IsNullOrEmpty(lblCodigoAluno.Text))
                checarSaldo.ChecarSaldoAluno(idComida, lstBar, lblSaldoAluno, lblTotal, qntItem);
            else MessageBox.Show("Selecione um item antes de prosseguir ou Insira um código aluno.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                var numeroStr = Microsoft.VisualBasic.Interaction.InputBox("Insira o código do aluno.", "Código Aluno");

                // Verifica se o valor inserido é nulo ou composto apenas de espaços em branco
                if (!string.IsNullOrWhiteSpace(numeroStr))
                {
                    int numero;

                    // Verifica se o valor inserido pode ser convertido para um número inteiro
                    if (int.TryParse(numeroStr, out numero))
                    {
                        using (MySqlConnection conexao = new MySqlConnection(Globais.data_source))
                        {
                            conexao.Open();
                            using (MySqlCommand cmd = new MySqlCommand())
                            {
                                cmd.Connection = conexao;
                                cmd.CommandText = @"SELECT N_Aluno, Nome_Aluno, Saldo FROM aluno
                                        WHERE N_Aluno = @codigo";
                                cmd.Parameters.AddWithValue("@codigo", numero);

                                using (MySqlDataReader reader = cmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        lblCodigoAluno.Text = reader.GetInt32(0).ToString();
                                        lblNomeAluno.Text = reader.GetString(1);
                                        lblSaldoAluno.Text = reader.GetDecimal(2).ToString("N2");

                                        lblCodigoAluno.Visible = true;
                                        lblNomeAluno.Visible = true;
                                        lblSaldoAluno.Visible = true;
                                    }

                                    if (!reader.HasRows)
                                    {
                                        MessageBox.Show("Código aluno não encontrado", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Insira somente números para o código do aluno", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Insira somente números para o código do aluno", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnFuncionario_Click(object sender, EventArgs e)
        {
            FormularioFuncionario f = new FormularioFuncionario();
            ChecarLogin(f);
        }

        private void btnComida_Click(object sender, EventArgs e)
        {
            FormularioComida f = new FormularioComida();       
            ChecarLogin(f);
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            limparTudo();
        }

        private void btnRemover_Click(object sender, EventArgs e)
        {
            GerirAcoesLstBar.ValorTotalRemovido(totalAcumulado,somarValorFaltante,lstBar,lblTotal,lblSaldoAluno);
        }

        private void timer1_Tick_1(object sender, EventArgs e)
        {
            this.lblHora.Text = DateTime.Now.ToString("yyyy-MM-dd : HH:mm:ss");
        }

        private void btnConcluir_Click(object sender, EventArgs e)
        {
            decimal total = 0;
            try
            {
                using (MySqlConnection conexao = new MySqlConnection(Globais.data_source))
                {
                    conexao.Open();

                    foreach (ListViewItem item in lstBar.Items)
                    {
                        string descricaoComida = item.SubItems[0].Text;
                        string valorString = item.SubItems[1].Text;
                        string quantidadeString = item.SubItems[2].Text;

                        if (decimal.TryParse(valorString, out decimal valor) && int.TryParse(quantidadeString, out int quantidade))
                        {
                            total = valor * quantidade;
                        }

                        using (MySqlCommand cmd = new MySqlCommand())
                        {
                            cmd.Connection = conexao;
                            cmd.CommandText = @"INSERT INTO bar (N_Aluno, Cod_Comida,Data_Compra,N_Funcionario, Valor_Gasto, Quantidade) 
                                        VALUES (@N_Aluno, @Cod_Comida, @data_compra, @N_Funcionario, @valorGasto, @quantidade)";

                            cmd.Parameters.AddWithValue("@N_Aluno", lblCodigoAluno.Text);
                            cmd.Parameters.AddWithValue("@Cod_Comida", obterIdComida(descricaoComida));
                            cmd.Parameters.AddWithValue("@data_compra", DateTime.Now);
                            cmd.Parameters.AddWithValue("@N_Funcionario", N_Funcionario);
                            cmd.Parameters.AddWithValue("@valorGasto", total);
                            cmd.Parameters.AddWithValue("@quantidade", quantidadeString);
                            cmd.ExecuteNonQuery();

                            BaseDados.AtualizarSaldoAluno(Convert.ToDecimal(lblSaldoAluno.Text), Convert.ToInt32(lblCodigoAluno.Text));
                        }
                    }
                }
                MessageBox.Show("Compra realizada com sucesso!", "Concluído", MessageBoxButtons.OK, MessageBoxIcon.Information);
                limparTudo();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        #endregion

        #region Functions

        //Reset all items 
        private void limparTudo()
        {
            lblCodigoAluno.ResetText();
            lblNomeAluno.ResetText();
            lblSaldoAluno.ResetText();
            lstBar.Items.Clear();
            lblTotal.Text = "0,00 €";
            qntItem.ResetText();
            cbItem.ResetText();
        }
      
        //Read the all items in the table "infocomida" and add in the combobox
        private void preencherCombo()
        {
            string sql = "SELECT * FROM infocomida";
            try
            {
                using (MySqlConnection conexao = new MySqlConnection(Globais.data_source))
                {
                    conexao.Open();
                   using(MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = conexao;
                        cmd.CommandText = sql;
                        using(MySqlDataReader reader = cmd.ExecuteReader())
                        {

                            while (reader.Read())
                            {
                                string nomeProduto = reader.GetString("Descricao_Comida");
                                decimal valorProduto = reader.GetDecimal("Valor_Comida");

                                // Concatena o nome do produto e seu valor em uma única string
                                string item = $"{nomeProduto} - {valorProduto.ToString("N2")}€";

                                // Adiciona o item ao ComboBox
                                cbItem.Items.Add(item);

                                // Configura o ValueMember do ComboBox
                                cbItem.ValueMember = reader["Cod_Comida"].ToString();
                            }
                            cbItem.DisplayMember = reader.GetString("Descricao_Comida");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }        
        
        private void ChecarLogin(Form f)
        {
            if (Globais.logado == true)
            {
                f.ShowDialog();
            }
            else MessageBox.Show("Necessário um login.");
        }
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //Keep the name food in the combo           
            string itemSelecionado = cbItem.SelectedItem.ToString();
            string nomeComida = itemSelecionado.Split('-')[0].Trim();
            try
            {
                using (MySqlConnection conexao = new MySqlConnection(Globais.data_source))
                {
                    conexao.Open();
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = conexao;
                        cmd.CommandText = "SELECT Cod_Comida FROM infocomida WHERE Descricao_Comida = @nomeComida";

                        //Remove the Cod_Comida (id) when the food name is equal to the variable
                        cmd.Parameters.AddWithValue("@nomeComida", nomeComida);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                //Set the id from select item in the combo box
                                idComida[0] = reader.GetInt32("Cod_Comida");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }   
        }

        private int obterIdComida(string descricao)
        {
            int codComida = -1;
            try
            {
                using (MySqlConnection conexao = new MySqlConnection(Globais.data_source))
                {
                    conexao.Open();
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = conexao;
                        cmd.CommandText = "SELECT Cod_Comida FROM infocomida WHERE Descricao_Comida = @descricao";
                        cmd.Parameters.AddWithValue("@descricao", descricao);

                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            codComida = Convert.ToInt32(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            return codComida;
        }
        #endregion

        #region Dont Used

        private void timer1_Tick(object sender, EventArgs e)
        {

        }

        private void lblHora_Click(object sender, EventArgs e)
        {

        }

        private void lstComida_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {

        }


        private void lblNome_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }
        #endregion

        private void btnEntrar_Click(object sender, EventArgs e)
        {
            if (btnEntrarSair.Text == "Entrar")
            {
                LoginFuncionario f = new LoginFuncionario(this, N_Funcionario);
                f.ShowDialog();
                btnEntrarSair.Text = "Sair";

            }
            else
            {
                btnEntrarSair.Text = "Entrar";
                pb_ledLogado.Image = Properties.Resources.led_vermelho;
                lblNome.Text = "---";
                Globais.logado = false;
                N_Funcionario = 0;
                limparTudo();
            }
        }

        private void lstComida_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
