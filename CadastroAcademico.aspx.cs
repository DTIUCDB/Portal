using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;
using DAL;
using Telerik.Web.UI;
using UCDB_SRV.Funcoes_SRV;
using UCDB_SRV.GerencialPeriodoLetivo_SRV;
using UCDB_DAL.CadastroBasico_DAL;
using UCDB_SRV;
using System.Text;
using System.IO;
using PortalWebApp.Classes;
using portal.Framework.Utility;
using PortalWebApp.Classes.IntegracaoSI.Modelos;
using ZIM;

public partial class GerencialPeriodoLetivo_CadastroAcademico : System.Web.UI.Page
{
    private const int PAIS_BRASIL_ID_SI = 31;
    private const int ID_RACA_PESSOA_NAO_QUIS_DECLARAR = 6;
    private const int ID_RACA_NAO_INFORMADA = 7;
    protected CadastroBasico_SRV servico = new CadastroBasico_SRV();
    protected GerencialPeriodoLetivo_SRV servicoPeriodoLetivo = new GerencialPeriodoLetivo_SRV();
    protected Funcoes_SRV funcoes = new Funcoes_SRV();
    protected wsLocalidades funcoesLocalidade = new wsLocalidades();
    protected IntegracaoMatricula integracaoMatriculaSI = new IntegracaoMatricula();
    Acesso acessoBD = new Acesso();
    private bool academicoEhCalouroDeGraduacao = false;
    private bool academicoEhDeLatoSensu = false;
    private bool academicoProvenienteDoCRM = false;

    protected DataTable SessaoGrdDocumentosPendentes
    {
        get
        {
            DataTable dt = (DataTable)Session["CadastroAcademicoGrdDocumentosPendentes"];

            if (dt == null)
            {
                dt = new DataTable();
                dt.Columns.Add("ID_PssDocPendente", typeof(string));
                dt.Columns.Add("ID_Documento", typeof(string));
                dt.Columns.Add("Obs", typeof(string));
            }

            return dt;
        }
        set
        {
            Session["CadastroAcademicoGrdDocumentosPendentes"] = value;
        }
    }

    protected DataTable SessaoGrdDeficiencia
    {
        get
        {
            DataTable dt = (DataTable)Session["CadastroAcademicoGrdDeficiencia"];

            if (dt == null)
            {
                dt = new DataTable();
                dt.Columns.Add("ID_tipoDeficiencia", typeof(string));
                dt.Columns.Add("Descricao", typeof(string));
            }

            return dt;
        }
        set
        {
            Session["CadastroAcademicoGrdDeficiencia"] = value;
        }
    }

    protected void limpaSessaoDesseCadastro()
    {
        Session.Remove("CadastroAcademicoGrdDeficiencia");
        Session.Remove("CadastroAcademicoGrdDocumentosPendentes");
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            // Caso o Academico tente acessar direto pela url.
            // Se os dois estiverem preenchidos... provavelmente.. é pq o colaborador simulou o academico no sistema.
            if (string.IsNullOrEmpty(Configuracoes.RF) && !string.IsNullOrEmpty(Configuracoes.RA))
                Response.Redirect("~/Default.aspx");

            limpaSessaoDesseCadastro();

            hfRA.Value = string.Empty;
            hfEhCadastro.Value = "true"; // Se for edição, TrataRedirecionamento() coloca como 'false'
            Configuracoes.PL_Matricula = Configuracoes.LerString("PLMatricula");
            Configuracoes.UsuarioZIM = servico.getCodUsuarioZIM(Configuracoes.RF);

            CarregaInformacoesPagina();
            TrataRedirecionamento();

            if (!string.IsNullOrEmpty(hfRA.Value))
            {
                academicoProvenienteDoCRM = AcademicoEhProvenienteDoCRM();
                academicoEhDeLatoSensu = AcademicoEhDeLatoSensu();
                academicoEhCalouroDeGraduacao = servicoPeriodoLetivo.AcademicoGraduacaoEhCalouro(Convert.ToInt32(hfRA.Value));
            }

            if (!string.IsNullOrEmpty(hfEhCadastro.Value) && hfEhCadastro.Value.Equals("true")) // Novo
            {
                grdDeficiencia.DataBind();
                grdDocumentosPendentes.DataBind();

                trEstadoNAOBrasileiro.Visible = false;
                trCidadeNAOBrasileiro.Visible = false;

                CarregaTelaBrasileiro();
                lblDadosAcadRA.Text = "Novo acadêmico";
            }
            else  // Edição
            {
                lblDadosAcadRA.Text = hfRA.Value;
                CarregaRegistroParaEdicao();
            }
        }
    }

    private bool AcademicoEhProvenienteDoCRM()
    {
        int ra = Convert.ToInt32(hfRA.Value);
        var vestibMatf = servicoPeriodoLetivo.ObterVestibMatf(ra);
        string numeroDeInscricao = vestibMatf.Rows.Count > 0 ? vestibMatf.Rows[0].Campo("NumeroInscricao") : string.Empty;
        return string.IsNullOrEmpty(numeroDeInscricao) == false;
    }

    private bool AcademicoEhDeLatoSensu()
    {
        DataTable dataTable = servicoPeriodoLetivo.ObterCursoDaMatricula(hfRA.Value);

        if (dataTable.Rows.Count > 0)
        {
            string grau = dataTable.Rows[0].Campo("c_grau");
            string sistemaCurricular = dataTable.Rows[0].Campo("sist_curricular");
            return grau.Equals("4") && sistemaCurricular.Equals("E");
        }

        return false;
    }

    protected void TrataRedirecionamento()
     {
        if (Request.QueryString.HasKeys())
        {
            if (!string.IsNullOrEmpty(Request.QueryString["d"]))
            {
                try
                {
                    string dadosCrytp = Request.QueryString["d"].ToString().Trim();

                    byte[] bytes = System.Convert.FromBase64String(dadosCrytp);
                    string dadosDecrytp = System.Text.ASCIIEncoding.ASCII.GetString(bytes);
                    string[] dadosAcademico = dadosDecrytp.Split(';');

                    hfEhCadastro.Value = "false";
                    hfRA.Value = dadosAcademico[0];
                    hfCPF.Value = dadosAcademico[1]; // Caso não tenha RA, é feita a busca do t_pss através do CPF
                    hfID_Curso.Value = dadosAcademico[2];

                    if (dadosAcademico.AsEnumerable().Where(p => p.Contains("matricula")).Count() > 0)
                        hfOrigem.Value = "matricula";

                    if (hfOrigem.Value.Equals("matricula"))
                    {
                        btnVoltar.Visible = false;

                        if (dadosAcademico.AsEnumerable().Where(p => p.Contains("problemaDados")).Count() > 0)
                        {
                            windowManager.RadAlert("O Acadêmico não possui os dados básicos necessários<br />para a realização da matrícula.<br /><br />Favor atualizar os dados do acadêmico,<br />como Nome, CPF, E-Mail e CURSO!", 350, 110, "ATENÇÃO!", null);
                        }
                    }
                }
                catch (Exception)
                {
                    Response.Redirect("ConsultaAcademico.aspx");
                }
            }
        }
    }

    protected void CarregaInformacoesPagina()
    {
        PreencheDDLCursos();
        PreencheDDLPolo();
        funcoes.carregaRadioButtonListEnum(typeof(ETipoNaturalidade), rblNaturalidade, true, string.Empty, string.Empty, false, false);
        PreencheDDLDadosAcadEstCivil();
        PreencheDDLDadosAcadNacionalidade();
        funcoes.carregaRadioButtonListEnum(typeof(ETipoEndereço), rblEnderecoTipoCorrespondencia, true, string.Empty, string.Empty, false, true);
        PreencheDDLUFs();
        PreencheDDLtextoCidade(ddlDadosAcadCidade);
        PreencheDDLDadosAcadFormaIngresso();
        PreencheDDLDadosAcadCorRaca();
        PreencheDDLtextoCidade(ddlDocumentosTituloEleitorCidade);
        PreencheDDLtextoCidade(ddlEnderecoResidencialCidade);
        PreencheDDLtextoCidade(ddlEnderecoCorrespondenciaCidade);
        PreencheDDLEnderecoCorrespondenciaFORAPais();
        PreencheDDLDocPendentesPendencias();
        PreencheDDLDeficiencia();
    }

    protected void PreencheDDLCursos()
    {
        ddlCursos.DataSource = servico.ListaTodosCursosAtivos();
        ddlCursos.DataBind();
        ddlCursos.Items.Insert(0, new RadComboBoxItem("SELECIONE", "0"));
    }

    protected void ddlCursos_ItemDataBound(object sender, RadComboBoxItemEventArgs e)
    {
        if (e.Item.DataItem != null)
        {
            e.Item.Text = ((DataRowView)e.Item.DataItem).Row["c_ident_cr"] + " - " + ((DataRowView)e.Item.DataItem).Row["n_completo_cr"] +
                          trataTurno(((DataRowView)e.Item.DataItem).Row["c_turno"].ToString()) + " (" + ((DataRowView)e.Item.DataItem).Row["campus"] + ")";

            e.Item.Value = ((DataRowView)e.Item.DataItem).Row["c_ident_cr"].ToString();
        }
    }

    protected string trataTurno(string c_turno)
    {
        if (string.IsNullOrEmpty(c_turno))
            return string.Empty;

        switch (c_turno)
        {
            case "M":
                return " (Matutino)";
            case "D":
                return " (Diurno)";
            case "V":
                return " (Vespertino)";
            case "I":
                return " (Integral)";
            case "N":
                return " (Noturno)";
            default:
                return string.Empty;
        }
    }

    protected void PreencheDDLPolo()
    {
        DataSet ds = servico.listaPolos();
        ddlPolo.DataTextField = "n_polo";
        ddlPolo.DataValueField = "c_polo";
        ddlPolo.DataSource = ds.Tables[0];
        ddlPolo.DataBind();
        ddlPolo.Items.Insert(0, new RadComboBoxItem("SELECIONE", "0"));
    }

    protected void PreencheDDLDadosAcadEstCivil()
    {
        DataSet dt = servico.listaEstadosCivil();
        ddlDadosAcadEstCivil.DataSource = dt;
        ddlDadosAcadEstCivil.DataTextField = "DS_DMN";
        ddlDadosAcadEstCivil.DataValueField = "VL_DMN";
        ddlDadosAcadEstCivil.DataBind();
        // Foi utilizado "-1", pois o "0" já está sendo utilizado por algum tipo.
        ddlDadosAcadEstCivil.Items.Insert(0, new RadComboBoxItem("SELECIONE", "-1"));
    }

    protected void PreencheDDLDadosAcadNacionalidade()
    {
        DataSet dt = servico.listaNacionalidades();
        ddlDadosAcadNacionalidade.DataSource = dt;
        ddlDadosAcadNacionalidade.DataTextField = "n_nacion";
        ddlDadosAcadNacionalidade.DataValueField = "c_nacion";
        ddlDadosAcadNacionalidade.DataBind();
        ddlDadosAcadNacionalidade.Items.Insert(0, new RadComboBoxItem("SELECIONE", "0"));
    }

    protected void PreencheDDLUFs()
    {
        List<T_EST> tabela = servico.ListaEstadosBrasil();

        PreencheDDLUF(ddlDadosAcadUF, tabela);
        PreencheDDLUF(ddlDocumentosRGOrgEmissorUF, tabela);
        PreencheDDLUF(ddlDocumentosTituloEleitorUF, tabela);
        PreencheDDLUF(ddlEnderecoResidencialUF, tabela);
        PreencheDDLUF(ddlEnderecoCorrespondenciaUF, tabela);
    }

    protected void PreencheDDLUF(RadComboBox ddl, List<T_EST> tabela)
    {
        ddl.DataTextField = "SG_EST";
        ddl.DataValueField = "CD_EST";
        ddl.DataSource = tabela;
        ddl.DataBind();
        ddl.Items.Insert(0, new RadComboBoxItem("UF", "0"));
    }

    protected void PreencheDDLtextoCidade(RadComboBox ddl)
    {
        ddl.Items.Clear();
        ddl.ClearSelection();
        ddl.Enabled = false;
        ddl.Items.Insert(0, new RadComboBoxItem("CIDADE", "0"));
        ddl.DataBind();
    }

    protected void PreencheDDLCidade(RadComboBox ddlCidade, RadComboBox ddlUF)
    {
        ddlCidade.Items.Clear();
        ddlCidade.ClearSelection();
        ddlCidade.Items.Insert(0, new RadComboBoxItem("CIDADE", "0"));
        ddlCidade.DataSource = servico.ListaCidades(Convert.ToDecimal(ddlUF.SelectedItem.Value));
        ddlCidade.Enabled = true;
        ddlCidade.DataTextField = "NM_CDD";
        ddlCidade.DataValueField = "CD_CDD";
        ddlCidade.DataBind();
    }

    protected void PreencheDDLEnderecoCorrespondenciaFORAPais()
    {
        ddlEnderecoCorrespondenciaFORAPais.Items.Insert(0, new RadComboBoxItem("PAÍS", "0"));
        ddlEnderecoCorrespondenciaFORAPais.DataSource = servico.listaPaises();
        ddlEnderecoCorrespondenciaFORAPais.DataTextField = "NM_PSA";
        ddlEnderecoCorrespondenciaFORAPais.DataValueField = "CD_PSA";
        ddlEnderecoCorrespondenciaFORAPais.DataBind();
    }

    protected void PreencheDDLDadosAcadFormaIngresso()
    {
        // quando for novo acadêmico EAD listar somente forma de ingresso PROUNI
        DataTable dt = servicoPeriodoLetivo.ListaPermissoesAcesso(Configuracoes.RF, 209);
        if (dt.Rows.Count > 0)
        {
            if (Request.QueryString.HasKeys() && !string.IsNullOrEmpty(Request.QueryString["d"]))
            {
                ddlDadosAcadFormaIngresso.DataSource = servico.listaFormasIngresso();
            }
            else
            {
                ddlDadosAcadFormaIngresso.DataSource = servico.listaFormasIngresso().Where(ee => ee.CD_FORMAINGRESSO == 8);
            }

            ddlDadosAcadFormaIngresso.Enabled = false;
        }
        else
        {
            ddlDadosAcadFormaIngresso.DataSource = servico.listaFormasIngresso();
            ddlDadosAcadFormaIngresso.Items.Insert(0, new RadComboBoxItem("SELECIONE", "0"));
        }

        ddlDadosAcadFormaIngresso.DataTextField = "DS_FORMAINGRESSO";
        ddlDadosAcadFormaIngresso.DataValueField = "VL_FORMAINGRESSO";
        ddlDadosAcadFormaIngresso.DataBind();
    }

    protected void PreencheDDLDadosAcadCorRaca()
    {
        ddlDadosAcadCorRaca.DataSource = servico.listaCorRaca();
        ddlDadosAcadCorRaca.DataTextField = "NM_COR_RACA";
        ddlDadosAcadCorRaca.DataValueField = "ID";
        ddlDadosAcadCorRaca.SelectedValue = "7";
        ddlDadosAcadCorRaca.DataBind();
    }

    protected void PreencheDDLDocPendentesPendencias()
    {
        ddlDocPendentesPendencias.DataSource = servico.ListaDocumentosPendentes();
        ddlDocPendentesPendencias.DataBind();
        ddlDocPendentesPendencias.Items.Insert(0, new RadComboBoxItem("SELECIONE", "0"));
    }

    protected void PreencheDDLDeficiencia()
    {
        ddlDeficiencia.DataSource = servico.listaTipoDeficiencia();
        ddlDeficiencia.DataBind();
        ddlDeficiencia.Items.Insert(0, new RadComboBoxItem("SELECIONE", "0"));
    }

    protected void CarregaTelaBrasileiro()
    {
        rblNaturalidade.SelectedValue = Convert.ToInt32(ETipoNaturalidade.Brasileiro).ToString();

        LimpaCamposTelaBrasileiro();

        chkRegistrado_Consulado.Visible = true;
        fsDocumentosPessoaisBrasileiroOuNaturalizado.Visible = true;
        fsDocumentosPessoaisEstrangeiro.Visible = false;
        divEnderecoTipoCorrespondencia.Visible = false;
        rblEnderecoTipoCorrespondencia.SelectedValue = Convert.ToInt32(ETipoEndereço.No_Brasil).ToString();
        fsEnderecoTipoCorrespondenciaBrasil.Visible = true;
        fsEnderecoTipoCorrespondenciaFORABrasil.Visible = false;
        ddlDadosAcadNacionalidade.Visible = false;
        lblDadosAcadNacionalidade.Visible = true;
    }

    protected void HabilitarConsulado()
    {
        trEstadoNAOBrasileiro.Visible = true;
        trCidadeNAOBrasileiro.Visible = true;

    }

    protected void LimpaCamposTelaBrasileiro()
    {
        //Se já estava visível, não limpo os campos para não apagar oque já foi preenchido.
        if (!trCidadeEstadoBrasileiro.Visible)
        {
            ddlDadosAcadUF.SelectedIndex = -1;
            ddlDadosAcadCidade.Enabled = false;
        }

        if (!fsDocumentosPessoaisBrasileiroOuNaturalizado.Visible)
            LimpaCamposfsDocumentosPessoaisBrasileiroOuNaturalizado();

        if (!fsEnderecoTipoCorrespondenciaBrasil.Visible)
            LimpaCamposfsEnderecoTipoCorrespondenciaBrasil();
    }

    protected void LimpaCamposfsDocumentosPessoaisBrasileiroOuNaturalizado()
    {
        txtDocumentosCPF.Text = string.Empty;
        txtDocumentosRG.Text = string.Empty;
        txtDocumentosRGOrgEmissor.Text = string.Empty;
        ddlDocumentosRGOrgEmissorUF.Text = string.Empty;
        dpDocumentosRGDataExpedicao.Clear();
        txtDocumentosTituloEleitorNumero.Text = string.Empty;
        txtDocumentosTituloEleitorZona.Text = string.Empty;
        ddlDocumentosTituloEleitorUF.SelectedIndex = -1;
        ddlDocumentosTituloEleitorCidade.SelectedIndex = -1;
        ddlDocumentosTituloEleitorCidade.Enabled = false;
        txtDocumentosMilitarNumero.Text = string.Empty;
        txtDocumentosMilitarSerie.Text = string.Empty;
        txtDocumentosMilitarComplemento.Text = string.Empty;
        txtDocumentosMilitarSituacao.Text = string.Empty;
        txtDocumentosCPFMae.Text = string.Empty;
        txtDocumentosCPFPai.Text = string.Empty;
        txtDocumentosCPFResp.Text = string.Empty;
    }

    protected void LimpaCamposfsEnderecoTipoCorrespondenciaBrasil()
    {
        txtEnderecoCorrespondenciaLogradouro.Text = string.Empty;
        txtEnderecoCorrespondenciaNumero.Text = string.Empty;
        txtEnderecoCorrespondenciaComplemento.Text = string.Empty;
        txtEnderecoCorrespondenciaBairro.Text = string.Empty;
        txtEnderecoCorrespondenciaCEP.Text = string.Empty;
        ddlEnderecoCorrespondenciaUF.SelectedIndex = -1;
        ddlEnderecoCorrespondenciaCidade.SelectedIndex = -1;
        ddlEnderecoCorrespondenciaCidade.Enabled = false;
    }

    protected void CarregaTelaNaturalizado()
    {
        rblNaturalidade.SelectedValue = Convert.ToInt32(ETipoNaturalidade.Naturalizado).ToString();

        LimpaCamposTelaNaturalizado();

        trCidadeEstadoBrasileiro.Visible = false;
        trEstadoNAOBrasileiro.Visible = true;
        trCidadeNAOBrasileiro.Visible = true;
        chkRegistrado_Consulado.Visible = false;
        fsDocumentosPessoaisBrasileiroOuNaturalizado.Visible = true;
        fsDocumentosPessoaisEstrangeiro.Visible = false;
        divEnderecoTipoCorrespondencia.Visible = false;
        rblEnderecoTipoCorrespondencia.SelectedValue = Convert.ToInt32(ETipoEndereço.No_Brasil).ToString();
        fsEnderecoTipoCorrespondenciaBrasil.Visible = true;
        fsEnderecoTipoCorrespondenciaFORABrasil.Visible = false;

        if (!ddlDadosAcadNacionalidade.Enabled)
            ddlDadosAcadNacionalidade.SelectedIndex = -1;

        ddlDadosAcadNacionalidade.Visible = false;
        lblDadosAcadNacionalidade.Visible = true;  // "10"; // c_nacion = 10 // n_nacion = BRASILEIRA // dbZim.dbo.PaisNacion
    }

    protected void LimpaCamposTelaNaturalizado()
    {
        //Se já estava visível, não limpo os campos para não apagar oque já foi preenchido.
        if (!trEstadoNAOBrasileiro.Visible)
            txtDadosAcadNAOBrEstado.Text = string.Empty;

        if (!trCidadeNAOBrasileiro.Visible)
            txtDadosAcadNAOBrCidade.Text = string.Empty;

        if (!fsDocumentosPessoaisBrasileiroOuNaturalizado.Visible)
            LimpaCamposfsDocumentosPessoaisBrasileiroOuNaturalizado();

        if (!fsEnderecoTipoCorrespondenciaBrasil.Visible)
            LimpaCamposfsEnderecoTipoCorrespondenciaBrasil();
    }

    protected void CarregaTelaEstrangeiro()
    {
        rblNaturalidade.SelectedValue = Convert.ToInt32(ETipoNaturalidade.Estrangeiro).ToString();

        LimpaCamposTelaEstrangeiro();

        trCidadeEstadoBrasileiro.Visible = false;
        trEstadoNAOBrasileiro.Visible = true;
        trCidadeNAOBrasileiro.Visible = true;
        chkRegistrado_Consulado.Visible = false;
        fsDocumentosPessoaisBrasileiroOuNaturalizado.Visible = false;
        fsDocumentosPessoaisEstrangeiro.Visible = true;
        divEnderecoTipoCorrespondencia.Visible = true;
        fsEnderecoTipoCorrespondenciaBrasil.Visible = true;
        fsEnderecoTipoCorrespondenciaFORABrasil.Visible = false;

        if (!ddlDadosAcadNacionalidade.Enabled)
            ddlDadosAcadNacionalidade.SelectedIndex = -1;

        ddlDadosAcadNacionalidade.Visible = true;
        lblDadosAcadNacionalidade.Visible = false;  // "10"; // c_nacion = 10 // n_nacion = BRASILEIRA // dbZim.dbo.PaisNacion
    }

    protected void LimpaCamposTelaEstrangeiro()
    {
        //Se já estava visível, não limpo os campos para não apagar oque já foi preenchido.
        if (!trEstadoNAOBrasileiro.Visible)
            txtDadosAcadNAOBrEstado.Text = string.Empty;

        if (!trCidadeNAOBrasileiro.Visible)
            txtDadosAcadNAOBrCidade.Text = string.Empty;

        if (!fsDocumentosPessoaisEstrangeiro.Visible)
            LimpaCamposfsDocumentosPessoaisEstrangeiro();

        if (!fsEnderecoTipoCorrespondenciaBrasil.Visible)
            LimpaCamposfsEnderecoTipoCorrespondenciaBrasil();
    }

    protected void LimpaCamposfsDocumentosPessoaisEstrangeiro()
    {
        txtDocumentosRNE.Text = string.Empty;
        txtDocumentosRNEOrgEmissor.Text = string.Empty;
        txtDocumentosCPFEstrangeiro.Text = string.Empty;
        txtDocumentosPassaporteNumero.Text = string.Empty;
        dpDocumentosPassaporteDataEmissao.Clear();
        dpDocumentosPassaporteDataValidade.Clear();
        txtDocumentosPassaportePais.Text = string.Empty;
    }

    protected void rblNaturalidade_SelectedIndexChanged(object sender, EventArgs e)
    {
        switch ((ETipoNaturalidade)Convert.ToInt32(rblNaturalidade.SelectedItem.Value))
        {
            case ETipoNaturalidade.Brasileiro:
                CarregaTelaBrasileiro();
                break;

            case ETipoNaturalidade.Naturalizado:
                CarregaTelaNaturalizado();
                break;

            case ETipoNaturalidade.Estrangeiro:
                CarregaTelaEstrangeiro();
                break;

            default:
                break;
        }
    }

    protected void ddlDadosAcadNacionalidade_ItemDataBound(object sender, RadComboBoxItemEventArgs e)
    {
        // A drop só esta sendo utilizada para acadêmicos com NATURALIDADE diferente de "Brasileira"
        // No momento de salvar, se for brasileiro será setada na mão (movendo 10 para o campo, sem utilizar a ddl).
        if (e.Item.Value.Equals("10"))
            e.Item.Visible = false;
    }

    protected void ddlDadosAcadRespFinanceiro_SelectedIndexChanged(object sender, RadComboBoxSelectedIndexChangedEventArgs e)
    {
        ////////////////////////////////
        // ddlDadosAcadRespFinanceiro //
        ////////////////////////////////
        // Text="O mesmo"   Value="A  //
        // Text="Mãe"       Value="M" //
        // Text="Pai"       Value="P" //
        // Text="Outro"     Value="O" //
        ////////////////////////////////
        if (ddlDadosAcadRespFinanceiro.SelectedValue == "O")
            trTxtDadosAcadNomeResponsavel.Visible = true;
        else
            trTxtDadosAcadNomeResponsavel.Visible = false;
    }

    protected void ddlDadosAcadUF_SelectedIndexChanged(object sender, RadComboBoxSelectedIndexChangedEventArgs e)
    {
        if (ddlDadosAcadUF.SelectedItem.Value != "0")
        {
            PreencheDDLCidade(ddlDadosAcadCidade, ddlDadosAcadUF);

            if (IsPostBack)
                RadAjaxManager1.FocusControl(ddlDadosAcadCidade.ClientID + "_Input");
        }
        else
            PreencheDDLtextoCidade(ddlDadosAcadCidade);
    }

    protected void ddlDocumentosTituloEleitorUF_SelectedIndexChanged(object sender, RadComboBoxSelectedIndexChangedEventArgs e)
    {
        if (ddlDocumentosTituloEleitorUF.SelectedItem.Value != "0")
        {
            PreencheDDLCidade(ddlDocumentosTituloEleitorCidade, ddlDocumentosTituloEleitorUF);
            if (IsPostBack)
                RadAjaxManager1.FocusControl(ddlDocumentosTituloEleitorCidade.ClientID + "_Input");
        }
        else
            PreencheDDLtextoCidade(ddlDocumentosTituloEleitorCidade);
    }

    protected void ddlEnderecoResidencialUF_SelectedIndexChanged(object sender, RadComboBoxSelectedIndexChangedEventArgs e)
    {
        if (ddlEnderecoResidencialUF.SelectedItem.Value != "0")
        {
            PreencheDDLCidade(ddlEnderecoResidencialCidade, ddlEnderecoResidencialUF);
            if (IsPostBack)
                RadAjaxManager1.FocusControl(ddlEnderecoResidencialCidade.ClientID + "_Input");
        }
        else
            PreencheDDLtextoCidade(ddlEnderecoResidencialCidade);
    }

    protected void ddlEnderecoCorrespondenciaUF_SelectedIndexChanged(object sender, RadComboBoxSelectedIndexChangedEventArgs e)
    {
        if (ddlEnderecoCorrespondenciaUF.SelectedItem.Value != "0")
        {
            PreencheDDLCidade(ddlEnderecoCorrespondenciaCidade, ddlEnderecoCorrespondenciaUF);
            if (IsPostBack)
                RadAjaxManager1.FocusControl(ddlEnderecoCorrespondenciaCidade.ClientID + "_Input");
        }
        else
            PreencheDDLtextoCidade(ddlEnderecoCorrespondenciaCidade);
    }

    protected void rblEnderecoTipoCorrespondencia_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (((ETipoEndereço)Convert.ToInt32(rblEnderecoTipoCorrespondencia.SelectedValue) == ETipoEndereço.No_Brasil))
        {
            fsEnderecoTipoCorrespondenciaBrasil.Visible = true;
            fsEnderecoTipoCorrespondenciaFORABrasil.Visible = false;
            LimpaCamposFsEnderecoTipoCorrespondenciaBrasil();
        }
        else
        {
            fsEnderecoTipoCorrespondenciaBrasil.Visible = false;
            fsEnderecoTipoCorrespondenciaFORABrasil.Visible = true;
            LimpaCamposFsEnderecoTipoCorrespondenciaFORABrasil();
        }
    }

    protected void LimpaCamposFsEnderecoTipoCorrespondenciaBrasil()
    {
        txtEnderecoCorrespondenciaLogradouro.Text = string.Empty;
        txtEnderecoCorrespondenciaNumero.Text = string.Empty;
        txtEnderecoCorrespondenciaComplemento.Text = string.Empty;
        txtEnderecoCorrespondenciaBairro.Text = string.Empty;
        txtEnderecoCorrespondenciaCEP.Text = string.Empty;
        ddlEnderecoCorrespondenciaUF.SelectedIndex = -1;
        ddlEnderecoCorrespondenciaCidade.SelectedIndex = -1;
    }

    protected void LimpaCamposFsEnderecoTipoCorrespondenciaFORABrasil()
    {
        txtEnderecoCorrespondenciaFORAEndereco.Text = string.Empty;
        txtEnderecoCorrespondenciaFORAEstadoDistrito.Text = string.Empty;
        txtEnderecoCorrespondenciaFORACidade.Text = string.Empty;
        txtEnderecoCorrespondenciaFORACodigoPostal.Text = string.Empty;
        ddlEnderecoCorrespondenciaFORAPais.SelectedIndex = -1;
    }

    protected void txtInformacoesIDEscola_TextChanged(object sender, EventArgs e)
    {
        if (!String.IsNullOrEmpty(txtInformacoesIDEscola.Text.Trim()))
        {
            DataTable dt = servico.listaPaseescolas(txtInformacoesIDEscola.Text.Trim(), string.Empty);
            if (dt == null)
            {
                txtInformacoesEscola.Text = string.Empty;
                txtInformacoesIDEscola.Text = string.Empty;
            }
            else
            {
                if (dt.Rows[0]["nome_escola"] != null)
                {
                    txtInformacoesEscola.Text = dt.Rows[0]["nome_escola"].ToString();
                }
                else
                {
                    txtInformacoesEscola.Text = string.Empty;
                    txtInformacoesIDEscola.Text = string.Empty;
                }
            }
        }
        else
        {
            txtInformacoesEscola.Text = string.Empty;
            txtInformacoesIDEscola.Text = string.Empty;
        }
    }

    protected void txtInformacoesIDIes_TextChanged(object sender, EventArgs e)
    {
        if (!String.IsNullOrEmpty(txtInformacoesIDIes.Text.Trim()))
        {
            DataTable dt = servico.listaHistEscola(txtInformacoesIDIes.Text.Trim(), string.Empty);
            if (dt == null)
            {
                txtInformacoesIDIes.Text = string.Empty;
                txtInformacoesIes.Text = string.Empty;
            }
            else
            {
                if (dt.Rows[0]["nome_escola"] != null)
                {
                    txtInformacoesIes.Text = dt.Rows[0]["nome_escola"].ToString();
                }
                else
                {
                    txtInformacoesIDIes.Text = string.Empty;
                    txtInformacoesIes.Text = string.Empty;
                }
            }

            if (!string.IsNullOrEmpty(txtInformacoesAnoVestibular.Text.Trim()))
            {
                buscaDisciplinasVestibular();
            }
        }
        else
        {
            txtInformacoesIDIes.Text = string.Empty;
            txtInformacoesIes.Text = string.Empty;
        }
    }

    protected void txtInformacoesAnoVestibular_TextChanged(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(txtInformacoesAnoVestibular.Text.Trim()) &&
            !String.IsNullOrEmpty(txtInformacoesIDIes.Text.Trim()))
        {
            buscaDisciplinasVestibular();
        }
    }

    protected void buscaDisciplinasVestibular()
    {
        if (txtInformacoesIDIes.Text.Trim().ToUpper().Trim() == "UCDB"
             || txtInformacoesIDIes.Text.Trim().ToUpper().Trim() == "FCMT"
             || txtInformacoesIDIes.Text.Trim().ToUpper().Trim() == "ENEM")
        {
            ZIM.ZimDAL zd = new ZIM.ZimDAL();
            DataTable dtMatVestib = zd.GetZIM("matvestibular", "ano_vestibular = '" + txtInformacoesAnoVestibular.Text.Trim().ToUpper() + "'").Tables[0];
            if (dtMatVestib.Rows.Count > 0)
                txtInformacoesDiscVest.Text = dtMatVestib.Rows[0]["mat_vestibular"].ToString();
        }
    }

    protected void txtInformacoesIDTransferido_TextChanged(object sender, EventArgs e)
    {
        if (!String.IsNullOrEmpty(txtInformacoesIDTransferido.Text.Trim()))
        {
            DataTable dt = servico.listaHistEscola(txtInformacoesIDTransferido.Text.Trim(), string.Empty);
            if (dt == null)
            {
                txtInformacoesIDTransferido.Text = string.Empty;
                txtInformacoesTranferido.Text = string.Empty;
            }
            else
            {
                if (dt.Rows[0]["nome_escola"] != null)
                {
                    txtInformacoesTranferido.Text = dt.Rows[0]["nome_escola"].ToString();
                }
                else
                {
                    txtInformacoesIDTransferido.Text = string.Empty;
                    txtInformacoesTranferido.Text = string.Empty;
                }
            }
        }
    }

    protected void btnModalBusca_Click(object sender, EventArgs e)
    {
        if (gvEscolas.Visible)
        {
            fechaModalAtual();
            gvEscolas.EmptyDataText = "Nenhum registro encontrado!";
            gvEscolas.DataSource = servico.listaPaseescolas(string.Empty, txtModalBusca.Text.Trim());
            abrirModalBusca();
            gvEscolas.DataBind();
        }
        else if (gvEscolasIES.Visible)
        {
            fechaModalAtual();
            gvEscolasIES.EmptyDataText = "Nenhum registro encontrado!";
            gvEscolasIES.DataSource = servico.listaHistEscola(string.Empty, txtModalBusca.Text.Trim());
            abrirModalBusca();
            gvEscolasIES.DataBind();
        }
        else if (gvTranferido.Visible)
        {
            fechaModalAtual();
            gvTranferido.EmptyDataText = "Nenhum registro encontrado!";
            gvTranferido.DataSource = servico.listaHistEscola(string.Empty, txtModalBusca.Text.Trim());
            abrirModalBusca();
            gvTranferido.DataBind();
        }
    }

    protected void mostraModalEscola(GridView grid)
    {
        gvEscolas.Visible = false;
        gvEscolasIES.Visible = false;
        gvTranferido.Visible = false;

        grid.Visible = true;
        abrirModalBusca();
    }

    protected void abrirModalBusca()
    {
        RadToolTipModalBusca.Show();
    }

    protected void limparGridEscolas(GridView grid)
    {
        grid.DataSource = null;
        grid.EmptyDataText = string.Empty;
        grid.DataBind();
    }

    protected void btnInformacoesBuscaEscola_Click(object sender, EventArgs e)
    {
        txtModalBusca.Text = string.Empty;
        limparGridEscolas(gvEscolas);
        RadToolTipModalBusca.Title = "Busca - Escola Fim E. Médio";
        mostraModalEscola(gvEscolas);
    }

    protected void gvEscolas_PageIndexChanging(object sender, GridViewPageEventArgs e)
    {
        gvEscolas.PageIndex = e.NewPageIndex;
        mostraModalEscola(gvEscolas);
        btnModalBusca_Click(null, null);
    }

    protected void gvEscolas_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e.CommandName == "selEscola")
        {
            string[] args = e.CommandArgument.ToString().Split('|');
            txtInformacoesIDEscola.Text = args[0];
            txtInformacoesEscola.Text = args[1];
            fechaModalAtual();
        }
        else if (e.CommandName == "editEscola")
        {
            string[] args = e.CommandArgument.ToString().Split('|');
            DataTable dt = servico.listaPaseescolas(args[0].PadLeft(5, '0'), string.Empty);
            if (dt != null)
            {
                if (dt.Rows[0] != null)
                {
                    ddlCadEscPNivel.EnableViewState = false;
                    ddlCadEscPDepAdm.EnableViewState = false;
                    ddlCadEscPConveni.EnableViewState = false;

                    ddlCadEscPNivel.SelectedIndex = 0;
                    ddlCadEscPDepAdm.SelectedIndex = 0;
                    ddlCadEscPConveni.SelectedIndex = 0;

                    if (!String.IsNullOrEmpty(dt.Rows[0]["estado"].ToString()) && !String.IsNullOrEmpty(dt.Rows[0]["cidade"].ToString()))
                    {
                        ccCadEscPEstado.SelectedValue = servico.ConsultaEstado(dt.Rows[0]["estado"].ToString());
                        ccCadEscPCidade.SelectedValue = servico.ConsultaCidade(dt.Rows[0]["cidade"].ToString(), dt.Rows[0]["estado"].ToString());

                        gnEscolaridadeBrasil.Checked = true;
                        CampoBrasil.Visible = true;

                        CampoEstrangeiroPais.Visible = false;
                        CampoEstrangeiroDistrito.Visible = false;
                        CampoEstrangeiroCidade.Visible = false;
                    }
                    else
                    {
                        gnEscolaridadeEstrangeiro.Checked = true;
                        CampoBrasil.Visible = false;

                        CampoEstrangeiroPais.Visible = true;
                        CampoEstrangeiroDistrito.Visible = true;
                        CampoEstrangeiroCidade.Visible = true;

                        //ddlCadEscPPaisEstran.SelectedValue = 
                        ccCadEscPPais.SelectedValue = servico.getPaisPorNome(dt.Rows[0]["nome_pais"].ToString());
                        txtCadEscPDistritoEstrang.Text = (dt.Rows[0]["nome_distrito"] == null) ? string.Empty : dt.Rows[0]["nome_distrito"].ToString();
                        txtCadEscPCidadeEstrang.Text = (dt.Rows[0]["nome_cidade"] == null) ? string.Empty : dt.Rows[0]["nome_cidade"].ToString();

                    }


                    //else  ////  MELHORIA ////  COLOCAR UM "SELECIONE" NAS DROPS DOS MODAIS DE ESCOLA //// ESSA PARTE FOI COPIADA DO CADASTRO ANTIGO
                    //{
                    //    ddlCadEscPEstado.Items.Insert(0, new ListItem() { Text = "SELECIONE", Value = "0" });
                    //    ddlCadEscPCidade.Items.Insert(0, new ListItem() { Text = "SELECIONE", Value = "0" });
                    //}

                    if (dt.Rows[0]["dep_adm"] != null)
                        ddlCadEscPDepAdm.SelectedValue = dt.Rows[0]["dep_adm"].ToString();
                    //else
                    //    ddlCadEscPDepAdm.Items.Insert(0, new ListItem() { Text = "SELECIONE", Value = "0" });

                    if (dt.Rows[0]["nivel_ens"] != null)
                        ddlCadEscPNivel.SelectedValue = dt.Rows[0]["nivel_ens"].ToString();
                    //else
                    //    ddlCadEscPNivel.Items.Insert(0, new ListItem() { Text = "SELECIONE", Value = "-1" });

                    if (dt.Rows[0]["i_convenio"] != null)
                        ddlCadEscPConveni.SelectedValue = dt.Rows[0]["i_convenio"].ToString();
                    //else
                    //    ddlCadEscPConveni.Items.Insert(0, new ListItem() { Text = "SELECIONE", Value = "0" });

                    txtCadEscPNome.Text = (dt.Rows[0]["nome_escola"] == null) ? string.Empty : dt.Rows[0]["nome_escola"].ToString();
                    txtCadEscPEndereco.Text = (dt.Rows[0]["endereco"] == null) ? string.Empty : dt.Rows[0]["endereco"].ToString();
                    txtCadEscPNumero.Text = (dt.Rows[0]["numero"] == null) ? string.Empty : dt.Rows[0]["numero"].ToString();
                    txtCadEscPCompl.Text = (dt.Rows[0]["complemento"] == null) ? string.Empty : dt.Rows[0]["complemento"].ToString();
                    txtCadEscPBairro.Text = (dt.Rows[0]["bairro"] == null) ? string.Empty : dt.Rows[0]["bairro"].ToString();
                    txtCadEscPCep.Text = (dt.Rows[0]["cep"] == null) ? string.Empty : dt.Rows[0]["cep"].ToString();
                    txtCadEscPTel.Text = (dt.Rows[0]["telefone"] == null) ? string.Empty : dt.Rows[0]["telefone"].ToString();
                    txtCadEscPEmail.Text = (dt.Rows[0]["email"] == null) ? string.Empty : dt.Rows[0]["email"].ToString();
                    hfC_Escola.Value = (dt.Rows[0]["c_escola"] == null) ? string.Empty : dt.Rows[0]["c_escola"].ToString();
                    lblErroCadastroEscP.Text = string.Empty;
                    fechaModalAtual();
                    abrirModalCadastroEscola();
                }
            }
        }
    }

    protected void btnInformacoesBuscaIES_Click(object sender, EventArgs e)
    {
        txtModalBusca.Text = string.Empty;
        limparGridEscolas(gvEscolasIES);
        RadToolTipModalBusca.Title = "Busca - IES Vestibular";
        mostraModalEscola(gvEscolasIES);
    }

    protected void gvEscolasIES_PageIndexChanging(object sender, GridViewPageEventArgs e)
    {
        gvEscolasIES.PageIndex = e.NewPageIndex;
        mostraModalEscola(gvEscolasIES);
        btnModalBusca_Click(null, null);
    }

    protected void gvEscolasIES_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e.CommandName == "selEscola")
        {
            string[] args = e.CommandArgument.ToString().Split('|');
            txtInformacoesIDIes.Text = args[0];
            txtInformacoesIes.Text = args[1];
            fechaModalAtual();
        }
        else if (e.CommandName == "editEscola")
        {
            string[] args = e.CommandArgument.ToString().Split('|');
            DataTable dt = servico.listaHistEscola(args[0].ToString(), string.Empty);
            if (dt != null)
            {
                if (!String.IsNullOrEmpty(dt.Rows[0]["estado_escola"].ToString()) &&
                    !String.IsNullOrEmpty(dt.Rows[0]["cidade_escola"].ToString()))
                {
                    ccCadEscIEstado.SelectedValue = servico.ConsultaEstado(dt.Rows[0]["estado_escola"].ToString());
                    ccCadEscICidade.SelectedValue = servico.ConsultaCidade(dt.Rows[0]["cidade_escola"].ToString(), dt.Rows[0]["estado_escola"].ToString());
                }

                //else  ////  MELHORIA ////  COLOCAR UM "SELECIONE" NAS DROPS DOS MODAIS DE ESCOLA //// ESSA PARTE FOI COPIADA DO CADASTRO ANTIGO
                //{
                //    ddlCadEscIEstado.Items.Insert(0, new ListItem() { Text = "SELECIONE", Value = "0" });
                //    ddlCadEscICidade.Items.Insert(0, new ListItem() { Text = "SELECIONE", Value = "0" });
                //}

                if (dt.Rows[0]["dep_adm"] != null)
                    ddlCadEscIDepAdm.SelectedValue = dt.Rows[0]["dep_adm"].ToString();

                if (dt.Rows[0]["i_grau_escola"] != null)
                    ddlCadEscIGrau.SelectedValue = dt.Rows[0]["i_grau_escola"].ToString();

                txtCadEscINome.Text = dt.Rows[0]["nome_escola"] == null ? string.Empty : dt.Rows[0]["nome_escola"].ToString();
                txtCadEscIDiretor.Text = dt.Rows[0]["nome_diretor"] == null ? string.Empty : dt.Rows[0]["nome_diretor"].ToString();
                txtCadEscISecret.Text = dt.Rows[0]["nome_secretario"] == null ? string.Empty : dt.Rows[0]["nome_secretario"].ToString();
                hfIdent_escola.Value = dt.Rows[0]["ident_escola"] == null ? string.Empty : dt.Rows[0]["ident_escola"].ToString();
                lblErroCadastroEscP.Text = string.Empty;
                fechaModalAtual();
                hfIesOuTransf.Value = "ies";
                abrirModalCadastroEscolaIesOuTransf();
            }
        }
    }

    protected void btnInformacoesBuscaTransferido_Click(object sender, EventArgs e)
    {
        txtModalBusca.Text = string.Empty;
        RadToolTipModalBusca.Title = "Busca - Transferido de";
        limparGridEscolas(gvTranferido);
        mostraModalEscola(gvTranferido);
    }

    protected void gvTranferido_PageIndexChanging(object sender, GridViewPageEventArgs e)
    {
        gvTranferido.PageIndex = e.NewPageIndex;
        mostraModalEscola(gvTranferido);
        btnModalBusca_Click(null, null);
    }

    protected void gvTranferido_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e.CommandName == "selEscola")
        {
            string[] args = e.CommandArgument.ToString().Split('|');
            txtInformacoesIDTransferido.Text = args[0];
            txtInformacoesTranferido.Text = args[1];
            fechaModalAtual();
        }
        else if (e.CommandName == "editEscola")
        {
            string[] args = e.CommandArgument.ToString().Split('|');
            DataTable dt = servico.listaHistEscola(args[0].ToString(), string.Empty);
            if (dt != null)
            {
                if (!String.IsNullOrEmpty(dt.Rows[0]["estado_escola"].ToString()) && !String.IsNullOrEmpty(dt.Rows[0]["cidade_escola"].ToString()))
                {
                    ccCadEscIEstado.SelectedValue = servico.ConsultaEstado(dt.Rows[0]["estado_escola"].ToString());
                    ccCadEscICidade.SelectedValue = servico.ConsultaCidade(dt.Rows[0]["cidade_escola"].ToString(), dt.Rows[0]["estado_escola"].ToString());
                }

                //else  ////  MELHORIA ////  COLOCAR UM "SELECIONE" NAS DROPS DOS MODAIS DE ESCOLA //// ESSA PARTE FOI COPIADA DO CADASTRO ANTIGO
                //{
                //    ddlCadEscIEstado.Items.Insert(0, new ListItem() { Text = "SELECIONE", Value = "0" });
                //    ddlCadEscICidade.Items.Insert(0, new ListItem() { Text = "SELECIONE", Value = "0" });
                //}

                if (dt.Rows[0]["dep_adm"] != null)
                    ddlCadEscIDepAdm.SelectedValue = dt.Rows[0]["dep_adm"].ToString();

                if (dt.Rows[0]["i_grau_escola"] != null)
                    ddlCadEscIGrau.SelectedValue = dt.Rows[0]["i_grau_escola"].ToString();

                txtCadEscINome.Text = dt.Rows[0]["nome_escola"] == null ? string.Empty : dt.Rows[0]["nome_escola"].ToString();
                txtCadEscIDiretor.Text = dt.Rows[0]["nome_diretor"] == null ? string.Empty : dt.Rows[0]["nome_diretor"].ToString();
                txtCadEscISecret.Text = dt.Rows[0]["nome_secretario"] == null ? string.Empty : dt.Rows[0]["nome_secretario"].ToString();
                hfIdent_escola.Value = dt.Rows[0]["ident_escola"] == null ? string.Empty : dt.Rows[0]["ident_escola"].ToString();
                lblErroCadastroEscP.Text = string.Empty;
                fechaModalAtual();
                hfIesOuTransf.Value = "transf";
                abrirModalCadastroEscolaIesOuTransf();
            }
        }
    }

    protected void abrirModalCadastroEscola()
    {

        RadToolTipModalCadastroEscola.Show();

    }

    protected void btnInformacoesAdicionaEscola_Click(object sender, EventArgs e)
    {

        hfC_Escola.Value = string.Empty;
        txtCadEscPNome.Text = string.Empty;
        ddlCadEscPNivel.SelectedIndex = 0;
        ddlCadEscPDepAdm.SelectedIndex = 0;
        txtCadEscPEndereco.Text = string.Empty;
        txtCadEscPNumero.Text = string.Empty;
        txtCadEscPCompl.Text = string.Empty;
        txtCadEscPBairro.Text = string.Empty;
        txtCadEscPCep.Text = string.Empty;
        txtCadEscPTel.Text = string.Empty;
        txtCadEscPEmail.Text = string.Empty;
        lblErroCadastroEscP.Text = string.Empty;
        ddlCadEscPConveni.SelectedIndex = 0;
        ccCadEscPEstado.SelectedValue = "-1";
        ccCadEscPCidade.SelectedValue = "-1";

        //Desabilita a EnableViewState para a DropDownList dos Países estrangeiros pois está trazendo valor caso a 1° ação seja de uma pesquisa
        ccCadEscPPais.EnableViewState = false;
        ccCadEscPPais.SelectedValue = "-1";

        txtCadEscPDistritoEstrang.Text = string.Empty;
        txtCadEscPCidadeEstrang.Text = string.Empty;

        gnEscolaridadeBrasil.Checked = true;
        gnEscolaridadeEstrangeiro.Checked = false;

        CampoBrasil.Visible = true;
        CampoEstrangeiroPais.Visible = false;
        CampoEstrangeiroDistrito.Visible = false;
        CampoEstrangeiroCidade.Visible = false;

        abrirModalCadastroEscola();
    }

    protected void btnModalCadastroEscolaCancelar_Click(object sender, EventArgs e)
    {
        fechaModalAtual();
    }

    protected void btnModalCadastroEscolaSalvar_Click(object sender, EventArgs e)
    {
        if (String.IsNullOrEmpty(txtCadEscPNome.Text))
        {
            lblErroCadastroEscP.Text = "* Informe o Nome <br />";
            abrirModalCadastroEscola();
            return;
        }

        if (gnEscolaridadeBrasil.Checked)
        {
            if (ddlCadEscPEstado.SelectedValue == "-1")
            {
                lblErroCadastroEscP.Text = "* Informe o Estado <br />";
                abrirModalCadastroEscola();
                return;
            }
            if (ddlCadEscPCidade.SelectedValue == "-1")
            {
                lblErroCadastroEscP.Text = "* Informe a Cidade <br />";
                abrirModalCadastroEscola();
                return;
            }
        }

        if (gnEscolaridadeEstrangeiro.Checked == true)
        {
            if (ddlCadEscPPaisEstran.SelectedValue == "-1")
            {
                lblErroCadastroEscP.Text = "* Informe o País <br />";
                abrirModalCadastroEscola();
                return;
            }
        }

        string c_escola = string.IsNullOrEmpty(hfC_Escola.Value) ? string.Empty : hfC_Escola.Value.Trim();
        string nome_escola = txtCadEscPNome.Text.Trim().ToUpper();
        string dep_adm = ddlCadEscPDepAdm.SelectedValue == "-1" ? "" : ddlCadEscPDepAdm.SelectedValue.Trim().ToUpper();
        string endereco = txtCadEscPEndereco.Text.Trim().ToUpper();
        string numero = txtCadEscPNumero.Text.Trim().ToUpper();
        string complemento = txtCadEscPCompl.Text.Trim().ToUpper();
        string bairro = txtCadEscPBairro.Text.Trim().ToUpper();
        string c_usuario = Configuracoes.UsuarioZIM;
        string d_atualizacao = DateTime.Now.ToString("yyyyMMdd");
        string cep = txtCadEscPCep.Text.Replace("-", "");
        string telefone = txtCadEscPTel.Text.Replace("-", "");
        string i_convenio = ddlCadEscPConveni.SelectedValue == "-1" ? "" : ddlCadEscPConveni.SelectedValue.Trim().ToUpper();
        string email = txtCadEscPEmail.Text.ToLower().Trim().ToUpper();
        string nivel_ens = ddlCadEscPNivel.SelectedValue == "-1" ? "" : ddlCadEscPNivel.SelectedValue.Trim().ToUpper();

        string cidade = null;
        string estado = null;

        string nomePaisEstrangeiro = null;
        string nomeDistritoEstrangeiro = null;
        string nomeCidadeEstrangeiro = null;

        if (gnEscolaridadeBrasil.Checked)
        {
            cidade = ddlCadEscPCidade.SelectedItem.Text.Trim().ToUpper();
            estado = ddlCadEscPEstado.SelectedItem.Text.Trim().ToUpper();
        }
        else if (gnEscolaridadeEstrangeiro.Checked)
        {
            nomePaisEstrangeiro = ddlCadEscPPaisEstran.SelectedItem.Text.Trim().ToUpper();
            nomeDistritoEstrangeiro = txtCadEscPDistritoEstrang.Text.Trim();
            nomeCidadeEstrangeiro = txtCadEscPCidadeEstrang.Text.Trim();
        }

        // Salva no Zim  e DBZim
        string c_escolaRetorno = string.Empty;
        if (!servico.SalvarPaseescolas(c_escola,
                                       nome_escola,
                                       cidade,
                                       dep_adm,
                                       endereco,
                                       numero,
                                       complemento,
                                       bairro,
                                       estado,
                                       c_usuario,
                                       d_atualizacao,
                                       cep,
                                       telefone,
                                       i_convenio,
                                       email,
                                       nivel_ens,
                                       nomePaisEstrangeiro,
                                       nomeDistritoEstrangeiro,
                                       nomeCidadeEstrangeiro,
                                       out c_escolaRetorno)) // Retorna o código novo ou o código do registro editado.
        {
            lblErroCadastroEscP.Text = "* ERRO AO SALVAR! INFORME DTI! <br />";
            btnModalCadastroEscolaSalvar.Enabled = false;
            abrirModalCadastroEscola();
        }
        else
        {
            txtInformacoesIDEscola.Text = c_escolaRetorno;
            txtInformacoesEscola.Text = nome_escola;
            fechaModalAtual();
        }
    }

    protected void btnInformacoesAdicionaIES_Click(object sender, EventArgs e)
    {
        txtCadEscINome.Text = string.Empty;
        ddlCadEscIGrau.SelectedIndex = -1;
        txtCadEscIDiretor.Text = string.Empty;
        txtCadEscISecret.Text = string.Empty;
        ddlCadEscIEstado.SelectedIndex = -1;
        ddlCadEscICidade.SelectedIndex = -1;
        ddlCadEscIDepAdm.SelectedIndex = -1;
        lblErroIES.Text = string.Empty;

        hfIesOuTransf.Value = "ies";
        abrirModalCadastroEscolaIesOuTransf();
    }

    protected void abrirModalCadastroEscolaIesOuTransf()
    {
        if (hfIesOuTransf.Value == "ies")
            RadToolTipModalCadastroEscolaIesOuTransf.Title = "Cadastro - IES";
        else if (hfIesOuTransf.Value == "transf")
            RadToolTipModalCadastroEscolaIesOuTransf.Title = "Cadastro - Transferido de";
        else
            RadToolTipModalCadastroEscolaIesOuTransf.Title = string.Empty;

        RadToolTipModalCadastroEscolaIesOuTransf.Show();
    }

    protected void fechaModalAtual()
    {
        this.RadAjaxManager1.ResponseScripts.Add("CloseToolTip();");
    }

    protected void btnModalCadastroEscolaIesOuTransfCancelar_Click(object sender, EventArgs e)
    {
        fechaModalAtual();
    }

    protected void btnModalCadastroEscolaIesOuTransfSalvar_Click(object sender, EventArgs e)
    {
        if (String.IsNullOrEmpty(txtCadEscINome.Text.Trim()))
        {
            lblErroCadastroEscP.Text = "* Informe o Nome <br />";
            abrirModalCadastroEscola();
            return;
        }

        string ident_escola = string.IsNullOrEmpty(hfIdent_escola.Value) ? string.Empty : hfIdent_escola.Value;
        string nome_escola = txtCadEscINome.Text.Trim().ToUpper();
        string i_grau_escola = ddlCadEscIGrau.SelectedValue.Trim();
        string nome_diretor = txtCadEscIDiretor.Text.Trim().ToUpper();
        string nome_secretario = txtCadEscISecret.Text.Trim().ToUpper();
        string cidade_escola = ddlCadEscICidade.SelectedItem.Text.Trim();
        string estado_escola = ddlCadEscIEstado.SelectedItem.Text.Trim();
        string c_usuario = Configuracoes.UsuarioZIM;
        string d_atualizacao = DateTime.Now.ToString("yyyyMMdd");
        string dep_adm = ddlCadEscIDepAdm.SelectedValue.Trim();

        // Salva no Zim e DBZim
        string ident_escolaRetorno = string.Empty;
        if (!servico.SalvarHistEscola(ident_escola,
                                      nome_escola,
                                      i_grau_escola,
                                      nome_diretor,
                                      nome_secretario,
                                      cidade_escola,
                                      estado_escola,
                                      c_usuario,
                                      d_atualizacao,
                                      dep_adm,
                                      out ident_escolaRetorno)) // Retorna o código novo ou o código do registro editado.
        {
            lblErroCadastroEscP.Text = "* ERRO AO SALVAR! INFORME DTI! <br />";
            btnModalCadastroEscolaIesOuTransfSalvar.Enabled = false;
            abrirModalCadastroEscolaIesOuTransf();
        }
        else
        {
            if (hfIesOuTransf.Value == "ies")
            {
                txtInformacoesIDIes.Text = ident_escolaRetorno;
                txtInformacoesIes.Text = nome_escola;
            }
            else if (hfIesOuTransf.Value == "transf")
            {
                txtInformacoesIDTransferido.Text = ident_escolaRetorno;
                txtInformacoesTranferido.Text = nome_escola;
            }

            fechaModalAtual();
        }
    }

    protected void btnInformacoesAdicionaTransferido_Click(object sender, EventArgs e)
    {
        txtCadEscINome.Text = string.Empty;
        ddlCadEscIGrau.SelectedIndex = -1;
        txtCadEscIDiretor.Text = string.Empty;
        txtCadEscISecret.Text = string.Empty;
        ddlCadEscIEstado.SelectedIndex = -1;
        ddlCadEscICidade.SelectedIndex = -1;
        ddlCadEscIDepAdm.SelectedIndex = -1;
        lblErroIES.Text = string.Empty;

        hfIesOuTransf.Value = "transf";
        abrirModalCadastroEscolaIesOuTransf();
    }

    protected void grdDocumentosPendentes_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        bool desabilitarExclusao = academicoProvenienteDoCRM && (academicoEhCalouroDeGraduacao || academicoEhDeLatoSensu);

        if (e.Row.Cells.Count >= 3 && desabilitarExclusao)
            e.Row.Cells[3].Visible = false;

        if (e.Row.RowType == DataControlRowType.DataRow || e.Row.RowType == DataControlRowType.Separator)
        {
            Label lbl = (Label)e.Row.FindControl("lblDocumentoNomeGrdDocumentosPendentes");
            int id = Convert.ToInt32(((DataRowView)e.Row.DataItem).Row.Field<string>("ID_Documento"));
            lbl.Text = (ddlDocPendentesPendencias.FindItemByValue(id.ToString())).Text;
        }
    }

    protected void grdDeficiencia_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e.Row.RowType == DataControlRowType.DataRow || e.Row.RowType == DataControlRowType.Separator)
        {
            Label lbl = (Label)e.Row.FindControl("lblTipoDeficienciaGrdDeficiencia");
            int id = Convert.ToInt32(((DataRowView)e.Row.DataItem).Row.Field<string>("Id_tipoDeficiencia"));
            lbl.Text = (ddlDeficiencia.FindItemByValue(id.ToString())).Text;
        }
    }

    protected void grdDocumentosPendentes_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e.CommandName == "Excluir")
        {
            DataTable colecao = SessaoGrdDocumentosPendentes;
            colecao.Rows.RemoveAt(Convert.ToInt32(e.CommandArgument.ToString()));
            SessaoGrdDocumentosPendentes = colecao;
            grdDocumentosPendentes.DataSource = SessaoGrdDocumentosPendentes;
            grdDocumentosPendentes.DataBind();
        }
    }

    protected void btnAdicionaDocPendente_Click(object sender, EventArgs e)
    {
        if (ddlDocPendentesPendencias.SelectedItem.Value == "0")
        {
            windowManager.RadAlert("Selecione o Tipo de Documento!", 350, 110, "ATENÇÃO!", null);
            return;
        }

        DataTable colecao = SessaoGrdDocumentosPendentes;

        bool existe = false;

        foreach (DataRow item in colecao.Rows)
        {
            if (item.Field<string>("ID_Documento") == ddlDocPendentesPendencias.SelectedItem.Value)
            {
                existe = true;
                break;
            }
        }

        if (existe)
        {
            windowManager.RadAlert("Documento selecionado já consta na lista!", 350, 110, "ATENÇÃO!", null);
            return;
        }

        colecao.Rows.Add(0,                                                             // ID_PssDocPendente int
                         Convert.ToInt32(ddlDocPendentesPendencias.SelectedItem.Value), // ID_Documento int 
                         txtDocPendenteObs.Text.Trim().ToUpper());                      // Obs string

        SessaoGrdDocumentosPendentes = colecao;
        grdDocumentosPendentes.DataSource = colecao;
        grdDocumentosPendentes.DataBind();
        ddlDocPendentesPendencias.SelectedIndex = -1;
        txtDocPendenteObs.Text = string.Empty;
    }

    protected void grdDeficiencia_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e.CommandName == "Excluir")
        {
            DataTable colecao = SessaoGrdDeficiencia;
            colecao.Rows.RemoveAt(Convert.ToInt32(e.CommandArgument.ToString()));
            SessaoGrdDeficiencia = colecao;
            grdDeficiencia.DataSource = SessaoGrdDeficiencia;
            grdDeficiencia.DataBind();
        }
    }

    protected void ddlDeficienciaAdicionar_Click(object sender, EventArgs e)
    {
        if (ddlDeficiencia.SelectedItem.Value == "0")
        {
            windowManager.RadAlert("Selecione o Tipo de Deficiência!", 350, 110, "ATENÇÃO!", null);
            return;
        }

        DataTable colecao = SessaoGrdDeficiencia;
        bool existe = false;

        foreach (DataRow item in colecao.Rows)
        {
            if (item.Field<string>("ID_tipoDeficiencia") == ddlDeficiencia.SelectedItem.Value)
            {
                existe = true;
                break;
            }
        }

        if (existe)
        {
            windowManager.RadAlert("Deficiência selecionada já consta na lista!", 350, 110, "ATENÇÃO!", null);
            return;
        }

        colecao.Rows.Add(ddlDeficiencia.SelectedItem.Value, // ID_tipoDeficiencia
                         ddlDeficiencia.SelectedItem.Text); // Descricao

        SessaoGrdDeficiencia = colecao;
        grdDeficiencia.DataSource = SessaoGrdDeficiencia;
        grdDeficiencia.DataBind();
        ddlDeficiencia.SelectedIndex = -1;
    }

    protected void CarregaRegistroParaEdicao()
    {
        // As informações estão sendo carregadas na ordem dos campos na tela.
        ddlCursos.Enabled = false;
        rblNaturalidade.Enabled = false;

        CadastroBasico_SRV servico = new CadastroBasico_SRV();
        T_ACD academico = servico.getAcademico(Convert.ToInt32(hfRA.Value));
        T_PSS pessoa;
        PessoaDadosComplementares academicoPessoaDadosComplementares;

        // Foi feito dessa forma, pois vindo da página de consulta acadêmico as vezes vem pessoas sem RA e podem possuir registro de pessoa.
        if (academico == null)
        {
            ddlCursos.Enabled = true;
            rblNaturalidade.Enabled = true;
            academico = new T_ACD();
            pessoa = servico.getPessoa(hfCPF.Value);
            lblDadosAcadRA.Text = "Novo acadêmico";

            if (pessoa == null)
            {
                // Carrega brasileiro por padrão
                // Se não possuir t_acd e nem T_PSS, não haverá informações a serem carregadas na tela.
                CarregaTelaBrasileiro();

                return;
            }
            else
            {
                academico.CD_PSS = pessoa.CD_PSS;
                chkRegistrado_Consulado.Checked = pessoa.BL_REGCONSULADO.Value != 0 ? true : false;

                rblNaturalidade.Enabled = pessoa.CD_NATURALIDADE != null ? false : true;
            }

            academicoPessoaDadosComplementares = pessoa.PessoaDadosComplementares
                                                       .Select(d => d as PessoaDadosComplementares)
                                                       .Where(d => d.CD_PSS == pessoa.CD_PSS).FirstOrDefault();

            if (academicoPessoaDadosComplementares == null)
            {
                academicoPessoaDadosComplementares = new PessoaDadosComplementares();
            }

            obtemInformacaoNaoTrazidaPelaImportacao(pessoa, academicoPessoaDadosComplementares);
        }
        else
        {
            pessoa = academico.T_PSS;
            rblNaturalidade.Enabled = pessoa.CD_NATURALIDADE != null ? false : true;

            if (pessoa.BL_REGCONSULADO.HasValue
                && pessoa.BL_REGCONSULADO.Value != 0)
            {
                chkRegistrado_Consulado.Checked = true;
                trEstadoNAOBrasileiro.Visible = true;
                trCidadeNAOBrasileiro.Visible = true;
                trCidadeEstadoBrasileiro.Visible = false;
                txtDadosAcadNAOBrEstado.Text = pessoa.NM_ESTADONASCESTR;
                txtDadosAcadNAOBrCidade.Text = pessoa.NM_CIDADENASCESTR;
            }
            else
            {
                chkRegistrado_Consulado.Checked = false;
                trEstadoNAOBrasileiro.Visible = false;
                trCidadeNAOBrasileiro.Visible = false;
                trCidadeEstadoBrasileiro.Visible = true;
            }

            academicoPessoaDadosComplementares = pessoa.PessoaDadosComplementares
                                                       .Select(o => o as PessoaDadosComplementares)
                                                       .Where(o => o.NR_ACD == academico.NR_ACD).FirstOrDefault();

            if (academicoPessoaDadosComplementares == null)
            {
                academicoPessoaDadosComplementares = new PessoaDadosComplementares();
            }

            obtemInformacaoNaoTrazidaPelaImportacao(pessoa, academicoPessoaDadosComplementares);
        }

        // Prepara a tela mostrando e escondendo os campos de acordo com o tipo.
        // Registros antigos não possuirão esse código  
        if (pessoa.CD_NATURALIDADE == null)
        {
            CarregaTelaBrasileiro();
        }
        else
        {
            switch ((ETipoNaturalidade)pessoa.CD_NATURALIDADE)
            {
                case ETipoNaturalidade.Brasileiro:
                    CarregaTelaBrasileiro();
                    break;
                case ETipoNaturalidade.Naturalizado:
                    CarregaTelaNaturalizado();
                    break;
                case ETipoNaturalidade.Estrangeiro:
                    CarregaTelaEstrangeiro();
                    break;
            }
        }

        if (pessoa.ID_COR_RACA != null)
        {
            ddlDadosAcadCorRaca.SelectedValue = pessoa.ID_COR_RACA.ToString();
        }
        else
        {
            ddlDadosAcadCorRaca.SelectedValue = "7";
        }

        if (string.IsNullOrEmpty(academicoPessoaDadosComplementares.ID_Curso))
        {
            // " - " é oque vem da página de consulta quando não existe curso cadastrado
            if (!string.IsNullOrEmpty(hfID_Curso.Value) && !hfID_Curso.Value.Equals(" - "))
                ddlCursos.SelectedValue = hfID_Curso.Value;
            else
                ddlCursos.Enabled = true;
        }
        else
        {
            if (hfEhCadastro.Value == "true")
                ddlCursos.Enabled = true;
            else
                ddlCursos.SelectedValue = hfID_Curso.Value.Equals(" - ") || string.IsNullOrEmpty(hfID_Curso.Value) ? academicoPessoaDadosComplementares.ID_Curso : hfID_Curso.Value;
        }

        if (hfEhCadastro.Value != "true")
            if (academicoPessoaDadosComplementares.ID_polo != null)
                ddlPolo.SelectedValue = ((int)academicoPessoaDadosComplementares.ID_polo).ToString("000");

        DataRow drNomeSocial = ObterNomeSocial(pessoa.CD_PSS);

        ckbNomeSocial.Checked = !string.IsNullOrWhiteSpace(drNomeSocial["POSSUI_NOME_SOCIAL"].ToString());
        txtNomeSocial.Text = drNomeSocial["NOME_SOCIAL"].ToString();

        if (ckbNomeSocial.Checked)
            nomeSocial.Visible = true;

        txtDadosAcadNome.Text = pessoa.NM_PSS;
        txtDadosAcadNomeMae.Text = pessoa.NM_MAE;
        txtDadosAcadNomePai.Text = pessoa.NM_PAI;

        if (!string.IsNullOrEmpty(pessoa.ID_SXE))
            rblDadosAcadSexo.SelectedValue = pessoa.ID_SXE.ToUpper();

        if (!string.IsNullOrEmpty(pessoa.ID_ESTCVL.Trim()))
            ddlDadosAcadEstCivil.SelectedValue = Convert.ToInt32(pessoa.ID_ESTCVL).ToString();

        dpDadosAcadNascimento.SelectedDate = pessoa.DT_NSC != null ? pessoa.DT_NSC.Value.AddHours(1) : pessoa.DT_NSC;
        //dpDadosAcadNascimento.Text = pessoa.DT_NSC != null ? ((DateTime)pessoa.DT_NSC).ToString("dd/MM/yyyy"): string.Empty;

        //seta combo nacionalidade quando estrsangeiro
        if (pessoa.CD_NATURALIDADE != null)
        {
            //busca nacionalidades
            DataSet dt = servico.listaNacionalidades();
            foreach (DataRow row in dt.Tables[0].Rows)
            {
                //encontrou a nacionalidade, seta na combo
                if (row[1].Equals(pessoa.DS_NCN))
                {
                    ddlDadosAcadNacionalidade.SelectedValue = row[0].ToString();
                    break;
                }
            }
        }
        else
        {
            ddlDadosAcadNacionalidade.SelectedValue = pessoa.CD_PSANSC == null ? "0" : pessoa.CD_PSANSC.ToString();
        }

        // Visibilidade está sendo feita no carregamento da tela.
        if (trCidadeEstadoBrasileiro.Visible)
        {
            //drop estado / cidade de nascimento
            if (!string.IsNullOrEmpty(pessoa.DS_NTR))
            {
                int posUltChar = pessoa.DS_NTR.LastIndexOf('-');
                string cidade = (posUltChar < 0) ? "" : pessoa.DS_NTR.Substring(0, posUltChar).ToUpper();
                string uf = (posUltChar < 0) ? "" : pessoa.DS_NTR.Substring(posUltChar + 1).ToUpper();

                ddlDadosAcadUF.SelectedIndex = ddlDadosAcadUF.FindItemIndexByText(uf, true);
                if (ddlDadosAcadUF.SelectedIndex != -1)
                {
                    ddlDadosAcadUF_SelectedIndexChanged(null, null);
                    ddlDadosAcadCidade.SelectedIndex = ddlDadosAcadCidade.FindItemIndexByText(cidade, true);
                }
            }
        }
        else
        {
            txtDadosAcadNAOBrEstado.Text = pessoa.NM_ESTADONASCESTR;
            txtDadosAcadNAOBrCidade.Text = pessoa.NM_CIDADENASCESTR;
        }

        if (!string.IsNullOrEmpty(academicoPessoaDadosComplementares.GrauParentesco))
        {
            ddlDadosAcadRespFinanceiro.SelectedValue = academicoPessoaDadosComplementares.GrauParentesco;
            ddlDadosAcadRespFinanceiro_SelectedIndexChanged(null, null);
            txtDadosAcadNomeResponsavel.Text = academicoPessoaDadosComplementares.NomeRespFinanceiro;
        }

        if (!string.IsNullOrEmpty(academico.ID_FRMING))
        {
            ddlDadosAcadFormaIngresso.SelectedValue = academico.ID_FRMING.ToUpper();
        }

        T_TLFPSS telefoneResidencial = pessoa.T_TLFPSS
                                             .Select(o => o as T_TLFPSS)
                                             .Where(o => (o.ID_TPITLF == Convert.ToInt32(ETelefone.Residencial).ToString()))
                                             .OrderByDescending(d => d.D_ATUALIZACAO).FirstOrDefault();
        if (telefoneResidencial != null)
        {
            txtTelefoneResidencialDDD.Text = telefoneResidencial.NR_DDD.ToString();
            txtTelefoneResidencialNumero.Text = telefoneResidencial.NR_TLF.ToString();
        }

        T_TLFPSS telefoneCelular = pessoa.T_TLFPSS
                                         .Select(o => o as T_TLFPSS)
                                         .Where(o => (o.ID_TPITLF == Convert.ToInt32(ETelefone.Celular).ToString()))
                                         .OrderByDescending(d => d.D_ATUALIZACAO).FirstOrDefault();
        if (telefoneCelular != null)
        {
            txtTelefoneCelularDDD.Text = telefoneCelular.NR_DDD.ToString();
            txtTelefoneCelularNumero.Text = telefoneCelular.NR_TLF.ToString().PadLeft(9, '0');
        }

        T_PSS_EML emailPrincipal = pessoa.T_PSS_EML
                                         .Select(o => o as T_PSS_EML)
                                         .Where(o => (o.TIPO == Convert.ToDecimal(EEmail.Principal)))
                                         .OrderByDescending(d => d.D_ATUALIAZACAO).FirstOrDefault();
        if (emailPrincipal != null)
            txtEmailPrincipal.Text = emailPrincipal.EMAIL;

        T_PSS_EML emailAlternativo = pessoa.T_PSS_EML
                                           .Select(o => o as T_PSS_EML)
                                           .Where(o => (o.TIPO == Convert.ToDecimal(EEmail.Alternativo)))
                                           .OrderByDescending(d => d.D_ATUALIAZACAO).FirstOrDefault();
        if (emailAlternativo != null)
            txtEmailAlternativo.Text = emailAlternativo.EMAIL;

        // Visibilidade está sendo feita no carregamento da tela.
        if (fsDocumentosPessoaisBrasileiroOuNaturalizado.Visible)
        {
            #region Documentos Pessoais - Documentos

            txtDocumentosCPF.Text = pessoa.NR_CPF;

            if (pessoa != null && pessoa.NR_CI != null)
                txtDocumentosRG.Text = pessoa.NR_CI.Trim();

            String[] orgaoExp = new String[2];
            if (pessoa.NM_ORGEXPCI != null && pessoa.NM_ORGEXPCI.Contains('/'))
            {
                orgaoExp = pessoa.NM_ORGEXPCI.Split('/');
            }
            else
            {
                orgaoExp[0] = pessoa.NM_ORGEXPCI;
                orgaoExp[1] = pessoa.SG_ESTCI;
            }
            txtDocumentosRGOrgEmissor.Text = orgaoExp[0];
            ddlDocumentosRGOrgEmissorUF.SelectedIndex = ddlDocumentosRGOrgEmissorUF.FindItemIndexByText(orgaoExp[1]);

            try
            {
                dpDocumentosRGDataExpedicao.SelectedDate = pessoa.DT_EXPCI;
            }
            catch (Exception)
            {
                dpDocumentosRGDataExpedicao.SelectedDate = null;
            }

            if (pessoa.NR_TTLELT != null && pessoa.NR_TTLELT != 0)
                txtDocumentosTituloEleitorNumero.Text = ((decimal)pessoa.NR_TTLELT).ToString("000000000000");

            if (pessoa.NR_ZNOTTL != null && pessoa.NR_ZNOTTL != 0)
                txtDocumentosTituloEleitorZona.Text = ((decimal)pessoa.NR_ZNOTTL).ToString("0000");

            if (!string.IsNullOrEmpty(pessoa.CD_ESTTTL))
            {
                // CD_ESTTTL não guarda o código. // Guarda o UF.
                string estttl = string.IsNullOrEmpty(pessoa.CD_ESTTTL) ? string.Empty : pessoa.CD_ESTTTL.ToUpper();
                ddlDocumentosTituloEleitorUF.SelectedIndex = ddlDocumentosTituloEleitorUF.FindItemIndexByText(estttl);

                if (ddlDocumentosTituloEleitorUF.SelectedIndex != -1)
                {
                    ddlDocumentosTituloEleitorUF_SelectedIndexChanged(null, null);

                    if (pessoa.CD_MNCTTL != null)
                        ddlDocumentosTituloEleitorCidade.SelectedValue = pessoa.CD_MNCTTL.ToString();
                }
            }
            #endregion

            #region Documentos Pessoais - Documento Militar

            txtDocumentosMilitarNumero.Text = academicoPessoaDadosComplementares.DocMilitar;
            txtDocumentosMilitarSerie.Text = academicoPessoaDadosComplementares.SerieMilitar;
            txtDocumentosMilitarComplemento.Text = academicoPessoaDadosComplementares.CompMilitar;
            txtDocumentosMilitarSituacao.Text = academicoPessoaDadosComplementares.SitMilitar;

            #endregion

            #region Documentos Pessoais - Outros Documentos

            txtDocumentosCPFMae.Text = academicoPessoaDadosComplementares.CPF_Mae;
            txtDocumentosCPFPai.Text = academicoPessoaDadosComplementares.CPF_Pai;
            txtDocumentosCPFResp.Text = academicoPessoaDadosComplementares.CPF_Resp;

            #endregion
        }
        else
        {
            #region Documentos Pessoais - Documentos (Estrangeiro)

            txtDocumentosRNE.Text = pessoa.NR_RNEESTR;
            txtDocumentosRNEOrgEmissor.Text = pessoa.NR_ORGRNEESTR;
            txtDocumentosCPFEstrangeiro.Text = pessoa.NR_CPF;

            #endregion

            #region Documentos Pessoais - Dados do Passaporte

            txtDocumentosPassaporteNumero.Text = pessoa.NR_PASSESTR;
            dpDocumentosPassaporteDataEmissao.SelectedDate = pessoa.DT_EMSPASSESTR;
            dpDocumentosPassaporteDataValidade.SelectedDate = pessoa.DT_VALPASSESTR;
            txtDocumentosPassaportePais.Text = pessoa.NM_PAISPASSESTR;

            #endregion
        }

        #region Endereço - Residencial

        T_ENDPSS enderecoResidencial = pessoa.T_ENDPSS
                                             .Select(o => o as T_ENDPSS)
                                             .Where(o => (o.CD_PSS == academico.CD_PSS) &&
                                                         (o.ID_TPIEND == Convert.ToDecimal(EEndereço.Residencial).ToString()))
                                            .OrderByDescending(d => d.D_ATUALIZACAO).FirstOrDefault();

        if (enderecoResidencial != null)
        {
            txtEnderecoResidencialLogradouro.Text = string.IsNullOrEmpty(enderecoResidencial.DS_END) ? string.Empty : enderecoResidencial.DS_END.ToUpper();

            // tem registros com valores não numéricos nesse campo número do endereço
            try
            {
                txtEnderecoResidencialNumero.Text = enderecoResidencial.NR_END;
            }
            catch (Exception)
            {
                txtEnderecoResidencialNumero.Text = string.Empty;
            }

            txtEnderecoResidencialComplemento.Text = enderecoResidencial.DS_CMPEND;
            txtEnderecoResidencialBairro.Text = enderecoResidencial.DS_BRR;

            if (enderecoResidencial.NR_CEP != null)
                txtEnderecoResidencialCEP.Text = enderecoResidencial.NR_CEP.ToString().PadLeft(8, '0');

            string sg = string.IsNullOrEmpty(enderecoResidencial.SG_EST) ? string.Empty : enderecoResidencial.SG_EST.ToUpper();
            ddlEnderecoResidencialUF.SelectedIndex = ddlEnderecoResidencialUF.FindItemIndexByText(sg);
            if (ddlEnderecoResidencialUF.SelectedIndex != -1)
            {
                ddlEnderecoResidencialUF_SelectedIndexChanged(null, null);
                //string lcl = string.IsNullOrEmpty(enderecoResidencial.DS_LCL) ? string.Empty : enderecoResidencial.DS_LCL.ToUpper();

                if (!String.IsNullOrEmpty(enderecoResidencial.DS_LCL) && !String.IsNullOrEmpty(enderecoResidencial.SG_EST))
                {
                    String sql = String.Format("SELECT NM_CDD FROM dbSII.SII.T_CDD WHERE NM_CDD like '{0}' AND SG_EST like '{1}'", enderecoResidencial.DS_LCL.Replace("'", "''"), enderecoResidencial.SG_EST);
                    DataSet ds = new Acesso().consulta(sql);
                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        String cidade = ds.Tables[0].Rows[0]["NM_CDD"].ToString();
                        ddlEnderecoResidencialCidade.SelectedIndex = ddlEnderecoResidencialCidade.FindItemIndexByText(cidade);
                    }
                }
            }
        }

        #endregion

        #region Endereço - Correspondência

        T_ENDPSS enderecoCorrespondencia = pessoa.T_ENDPSS
                                                 .Select(o => o as T_ENDPSS)
                                                 .Where(o => (o.CD_PSS == academico.CD_PSS) &&
                                                             (o.ID_TPIEND == Convert.ToDecimal(EEndereço.Correspondencia).ToString()))
                                                 .OrderByDescending(d => d.D_ATUALIZACAO).FirstOrDefault();

        if (enderecoCorrespondencia == null)
        {
            rblEnderecoTipoCorrespondencia.SelectedValue = Convert.ToInt32(ETipoEndereço.No_Brasil).ToString();
            rblEnderecoTipoCorrespondencia_SelectedIndexChanged(null, null);
        }
        else
        {
            if (enderecoCorrespondencia.FL_ENDBRASIL == null)
                rblEnderecoTipoCorrespondencia.SelectedValue = Convert.ToInt32(ETipoEndereço.No_Brasil).ToString();
            else if ((bool)enderecoCorrespondencia.FL_ENDBRASIL)
                rblEnderecoTipoCorrespondencia.SelectedValue = Convert.ToInt32(ETipoEndereço.No_Brasil).ToString();
            else
                rblEnderecoTipoCorrespondencia.SelectedValue = Convert.ToInt32(ETipoEndereço.Fora_do_Brasil).ToString();

            rblEnderecoTipoCorrespondencia_SelectedIndexChanged(null, null);

            if (rblEnderecoTipoCorrespondencia.SelectedItem.Value == Convert.ToInt32(ETipoEndereço.No_Brasil).ToString())
            {
                txtEnderecoCorrespondenciaLogradouro.Text = enderecoCorrespondencia.DS_END.ToUpper();

                // tem registros com valores não numéricos nesse campo número do endereço
                try
                {
                    txtEnderecoCorrespondenciaNumero.Text = enderecoCorrespondencia.NR_END;
                }
                catch (Exception)
                {
                    txtEnderecoCorrespondenciaNumero.Text = string.Empty;
                }

                txtEnderecoCorrespondenciaComplemento.Text = enderecoCorrespondencia.DS_CMPEND;
                txtEnderecoCorrespondenciaBairro.Text = enderecoCorrespondencia.DS_BRR;

                if (enderecoCorrespondencia.NR_CEP != null)
                    txtEnderecoCorrespondenciaCEP.Text = enderecoCorrespondencia.NR_CEP.ToString().PadLeft(8, '0');

                string sg = string.IsNullOrEmpty(enderecoCorrespondencia.SG_EST) ? string.Empty : enderecoCorrespondencia.SG_EST.ToUpper();
                ddlEnderecoCorrespondenciaUF.SelectedIndex = ddlEnderecoCorrespondenciaUF.FindItemIndexByText(sg);
                if (ddlEnderecoCorrespondenciaUF.SelectedIndex != -1)
                {
                    ddlEnderecoCorrespondenciaUF_SelectedIndexChanged(null, null);
                    string lcl = string.IsNullOrEmpty(enderecoCorrespondencia.DS_LCL) ? string.Empty : enderecoCorrespondencia.DS_LCL.ToUpper();
                    ddlEnderecoCorrespondenciaCidade.SelectedIndex = ddlEnderecoCorrespondenciaCidade.FindItemIndexByText(lcl);
                }
            }
            else if (rblEnderecoTipoCorrespondencia.SelectedItem.Value == Convert.ToInt32(ETipoEndereço.Fora_do_Brasil).ToString())
            {
                txtEnderecoCorrespondenciaFORAEndereco.Text = enderecoCorrespondencia.DS_END.ToUpper();
                txtEnderecoCorrespondenciaFORAEstadoDistrito.Text = enderecoCorrespondencia.DS_ESTESTR;
                txtEnderecoCorrespondenciaFORACidade.Text = enderecoCorrespondencia.DS_LCL;
                txtEnderecoCorrespondenciaFORACodigoPostal.Text = enderecoCorrespondencia.NR_CEP != null ? enderecoCorrespondencia.NR_CEP.ToString() : string.Empty;

                if (enderecoCorrespondencia.CD_PSA != null)
                    ddlEnderecoCorrespondenciaFORAPais.SelectedValue = enderecoCorrespondencia.CD_PSA.ToString();
            }
        }
        #endregion

        #region Informações Escolares e Acadêmicas
        txtInformacoesIDEscola.Text = academicoPessoaDadosComplementares.ID_Escola;
        txtInformacoesIDEscola_TextChanged(null, null);

        if (academicoPessoaDadosComplementares.AnoConclusao != null)
            if (academicoPessoaDadosComplementares.AnoConclusao != 0)
                txtInformacoesEscolaAnoConclusao.Text = academicoPessoaDadosComplementares.AnoConclusao.ToString();

        txtInformacoesIDIes.Text = academicoPessoaDadosComplementares.ID_EscolaVestib;
        txtInformacoesIDIes_TextChanged(null, null);
        txtInformacoesAnoVestibular.Text = academicoPessoaDadosComplementares.AnoVestibular;

        txtInformacoesDiscVest.Text = academicoPessoaDadosComplementares.DiscVestibular;

        txtInformacoesIDTransferido.Text = academicoPessoaDadosComplementares.ID_EscolaTransf;
        txtInformacoesIDTransferido_TextChanged(null, null);
        #endregion

        #region Prepara Grid de Documentos Pendentes

        DataTable dtDocumentosPendentes = new DataTable();
        dtDocumentosPendentes.Columns.Add("ID_PssDocPendente", typeof(string));
        dtDocumentosPendentes.Columns.Add("ID_Documento", typeof(string));
        dtDocumentosPendentes.Columns.Add("Obs", typeof(string));
        var colecaoDocumentosPendentes = (from pdp in pessoa.PessoaDocPendentes.AsEnumerable() where pdp.NR_ACD == academico.NR_ACD select pdp).ToList();

        foreach (PessoaDocPendentes item in colecaoDocumentosPendentes)
            dtDocumentosPendentes.Rows.Add(item.ID_PssDocPendente,
                                           item.ID_Documento,
                                           item.Obs);

        SessaoGrdDocumentosPendentes = dtDocumentosPendentes;
        grdDocumentosPendentes.DataSource = SessaoGrdDocumentosPendentes;
        grdDocumentosPendentes.DataBind();

        bool exibirMensagem = academicoProvenienteDoCRM && (academicoEhCalouroDeGraduacao || academicoEhDeLatoSensu);

        if (exibirMensagem)
        {
            divDocPendentes.Visible = false;
            divInfo.Visible = true;
            lblInformacao.Text = @"ATENÇÃO: Os documentos de calouros provenientes do CRM devem ser registrados no sistema
                                   de gerenciamento de documentos (Menu: SII/Acadêmico/Análise da Documentação de Ingresso)";
        }

        #endregion

        #region Prepara Grid de Deficiências
        DataTable dtDeficiencia = new DataTable();
        dtDeficiencia.Columns.Add("ID_tipoDeficiencia", typeof(string));
        dtDeficiencia.Columns.Add("Descricao", typeof(string));
        var colecaoDeficiencia = (from pdc in pessoa.PessoaDadosComplementares_Deficiencia where pdc.NR_ACD == academico.NR_ACD select pdc).ToList();
        foreach (PessoaDadosComplementares_Deficiencia item in colecaoDeficiencia)
            dtDeficiencia.Rows.Add(item.Id_tipoDeficiencia.ToString(),
                                   item.Pessoa_TipoDeficiencia.Descricao);
        SessaoGrdDeficiencia = dtDeficiencia;
        grdDeficiencia.DataSource = SessaoGrdDeficiencia;
        grdDeficiencia.DataBind();
        #endregion
    }

    private DataRow ObterNomeSocial(decimal CD_PSS)
    {
        Dictionary<string, object> parametros = new Dictionary<string, object>();
        parametros.Add("CD_PSS", CD_PSS);

        string sql = @"SELECT NOME_SOCIAL, POSSUI_NOME_SOCIAL  
                       FROM dbSII.SII.T_PSS
                       WHERE CD_PSS=@CD_PSS";

        var dsNomeSocial = acessoBD.consulta(sql, parametros);

        return dsNomeSocial.Tables[0].Rows[0];
    }

    protected void obtemInformacaoNaoTrazidaPelaImportacao(T_PSS pessoa, PessoaDadosComplementares academicoPessoaDadosComplementares)
    {
        DataSet dsDadosCompl = servico.getDadosComplementares(hfRA.Value);

        if (dsDadosCompl != null && dsDadosCompl.Tables.Count > 0 && dsDadosCompl.Tables[0].Rows.Count > 0)
        {
            DataRow row = dsDadosCompl.Tables[0].Rows[0];

            try
            {
                if (academicoPessoaDadosComplementares.ID_polo == null)
                    if (!string.IsNullOrEmpty(row.Campo("c_polo")))
                        academicoPessoaDadosComplementares.ID_polo = Convert.ToInt32(row.Campo("c_polo"));
            }
            catch (Exception)
            {
                academicoPessoaDadosComplementares.ID_polo = null;
            }

            if (string.IsNullOrWhiteSpace(pessoa.ID_ESTCVL))
                pessoa.ID_ESTCVL = "00"; // Não existe: vestib(ins_pg.php) e novapessoa.aspx

            if (string.IsNullOrWhiteSpace(pessoa.DS_NTR) || !pessoa.DS_NTR.Contains("-"))
                if (!string.IsNullOrEmpty(row.Campo("n_cidade_nasc")) && !string.IsNullOrEmpty(row.Campo("c_estado_nasc")))
                    pessoa.DS_NTR = row.Campo("n_cidade_nasc") + "-" + row.Campo("c_estado_nasc");

            txtEmailPrincipal.Text = row.Campo("e_mail");
            //txtEmailAlternativo.Text = "ra" + hfRA.Value + "@ucdb.br";

            if (string.IsNullOrWhiteSpace(pessoa.NR_CI))
                if (!string.IsNullOrEmpty(row.Campo("c_rg")))
                    pessoa.NR_CI = row.Campo("c_rg").Trim();


            if (string.IsNullOrWhiteSpace(pessoa.NM_ORGEXPCI))
                if (!string.IsNullOrEmpty(row.Campo("c_orgao_exp_rg")))
                    pessoa.NM_ORGEXPCI = row.Campo("c_orgao_exp_rg").Trim();

            if (string.IsNullOrWhiteSpace(pessoa.SG_ESTCI))
                if (!string.IsNullOrEmpty(row.Campo("c_estado_exp_rg")))
                    pessoa.SG_ESTCI = row.Campo("c_estado_exp_rg");

            try
            {
                if (pessoa.DT_EXPCI == null)
                    if (!string.IsNullOrEmpty(row.Campo("d_exp_rg")))
                        pessoa.DT_EXPCI = new DateTime(Convert.ToInt32(row.Campo("d_exp_rg").Substring(0, 4)),
                                                       Convert.ToInt32(row.Campo("d_exp_rg").Substring(4, 2)),
                                                       Convert.ToInt32(row.Campo("d_exp_rg").Substring(6, 2)));
            }
            catch (Exception)
            {
                pessoa.DT_EXPCI = null;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(pessoa.NR_CPF))
                    if (!string.IsNullOrEmpty(row.Campo("cpf")))
                        pessoa.NR_CPF = Convert.ToDecimal(row.Campo("cpf")).ToString("000.000.000-00");
            }
            catch (Exception)
            {
                pessoa.NR_CPF = string.Empty;
            }
        }
    }

    protected void btnVoltar_Click(object sender, EventArgs e)
    {
        windowManager.Localization.OK = "Sim";
        windowManager.Localization.Cancel = "Não";
        windowManager.RadConfirm("Deseja realmente voltar para a tela de consulta?", "confirmVoltaPaginaConsultaAcademico", 400, 100, null, "Atenção!");
    }

    protected void btnVoltaPaginaConsultaAcademico_Click(object sender, EventArgs e)
    {
        string commandArgument = Request.Params.Get("__EVENTARGUMENT").ToString();

        if (!string.IsNullOrEmpty(commandArgument))
        {
            if (commandArgument.Equals("true"))
            {
                Response.Redirect("ConsultaAcademico.aspx");
            }
        }
    }

    protected void btnSalvar_Click(object sender, EventArgs e)
    {
        if (!ValidaInformacoes())
            return;

        T_PSS pessoa;

        try
        {
            pessoa = CarregaObjetoParaSalvar();
        }
        catch (Exception)
        {
            LiberaRaReservadoNoZIM();
            windowManager.RadAlert("Ocorreu um erro ao carregar as informações para serem salvas!", 350, 110, "ATENÇÃO!", null);
            return;
        }

        try
        {
            if (servico.salvaPessoa(pessoa, hfRA.Value))
            {
                try
                {
                    SalvarNomeSocial(pessoa.CD_PSS);
                    SalvarPessoaNoSI(pessoa);
                }
                catch (Exception ex)
                {
                    enviaEmailErroParaDTI(ex, "Portal(SQL Server)/Pessoa", hfRA.Value);
                }

                try
                {
                    // Cursos do Portal Educação não geram registros no MatHistoricoAcademico.
                    if (!servico.validaCursoPortalEducacao(ddlCursos.SelectedValue.Trim()))
                        trataMatHistoricoAcademico(pessoa, Convert.ToDecimal(hfRA.Value));
                }
                catch (Exception ex)
                {
                    enviaEmailErroParaDTI(ex, "DbSII(SQL Server)/MatHistoricoAcademico", hfRA.Value);
                }

                try
                {
                    servico.SalvarAcademicoDbzimMysqlZim(pessoa, null, hfRA.Value, Configuracoes.UsuarioZIM);
                }
                catch (Exception ex)
                {
                    enviaEmailErroParaDTI(ex, "DbZim(SQL Server), MySQL ou ZIM", hfRA.Value);
                }

                if (hfOrigem.Value.Equals("matricula"))
                {
                    //ScriptManager.RegisterStartupScript(Page, GetType(), "fechaJanela", "fechaJanela();", true);
                    btnSalvar.Enabled = false;
                    this.RadAjaxManager1.ResponseScripts.Add("fechaJanela();");
                    return;
                }

                if (!string.IsNullOrEmpty(hfEhCadastro.Value) && hfEhCadastro.Value.Equals("true"))
                {
                    windowManager.Localization.OK = "OK";
                    windowManager.Localization.Cancel = "Cancel";

                    #region Se precisar recarregar a página após o salvar, descomentar essa parte.
                    // hidden - Utilizado no javascript para recarregar a tela para edição. // "redirecionaCadastro" recarrega a página e volta como edição.
                    //hfRecarregaPaginaParaEdicao.Value = criptografaDados(hfRA.Value + ";" + 0 + ";" + 0 + ";");
                    //windowManager.RadAlert("Acadêmico cadastrado com sucesso!", 350, 110, "SUCESSO!", "redirecionaCadastro");
                    #endregion

                    windowManager.RadAlert("Acadêmico cadastrado com sucesso!<br>RA: " + hfRA.Value, 350, 110, "SUCESSO!", "redirecionaConsultaAcademico", "../images/CheckEmailEAD.png");
                    ZIM.PST.rAlunosPST raluno = new ZIM.PST.rAlunosPST();
                    raluno.atualiza(hfRA.Value, "O", hfRA.Value);
                    hfEhCadastro.Value = "false";
                    return;
                }
                else
                {
                    windowManager.Localization.OK = "OK";
                    windowManager.Localization.Cancel = "Cancel";
                    //windowManager.RadAlert("Acadêmico atualizado com sucesso!", 350, 110, "SUCESSO!", null);
                    windowManager.RadAlert("Acadêmico atualizado com sucesso!<br><br>RA: " + hfRA.Value + "<br><br>", 350, 110, "SUCESSO!", "redirecionaConsultaAcademico", "../images/CheckEmailEAD.png");
                    return;
                }
            }
            else
            {
                LiberaRaReservadoNoZIM();
                windowManager.RadAlert("Ocorreu um erro ao cadastrar! Informe o DTI!", 350, 110, "ATENÇÃO!", null);
                return;
            }
        }
        catch (Exception ex)
        {
            LiberaRaReservadoNoZIM();
            enviaEmailErroParaDTI(ex, "DbSII(SQL Server)", hfRA.Value);
            windowManager.RadAlert("Ocorreu um erro ao cadastrar! Informe o DTI!", 350, 110, "ATENÇÃO!", null);
            return;
        }
    }

    private void SalvarNomeSocial(decimal CD_PSS)
    {
        string nome_social = string.Empty;
        bool possui_nome_social = false;

        if (ckbNomeSocial.Checked)
        {
            nome_social = txtNomeSocial.Text.Trim().ToUpper();
            possui_nome_social = ckbNomeSocial.Checked;
        }
        else
        {
            nome_social = null;
            possui_nome_social = false;
        }

        SalvarNomeSocialNoDbSII(CD_PSS, nome_social, possui_nome_social);
        SalvarNomeSocialNoDbZIM(CD_PSS, nome_social);
        SalvarNomeSocialNoZIM(CD_PSS, nome_social);
    }

    private void SalvarNomeSocialNoDbSII(decimal CD_PSS, string nome_social, bool possui_nome_social)
    {
        Dictionary<string, object> parametros = new Dictionary<string, object>();
        parametros.Add("CD_PSS", CD_PSS);
        parametros.Add("NOME_SOCIAL", nome_social);
        parametros.Add("possui_nome_social", possui_nome_social);

        string sql = @"UPDATE dbSII.SII.T_PSS
                       SET NOME_SOCIAL = @nome_social, POSSUI_NOME_SOCIAL= @possui_nome_social
                       WHERE CD_PSS=@CD_PSS";

        acessoBD.executar(sql, parametros);
    }

    private void SalvarNomeSocialNoDbZIM(decimal CD_PSS, string nome_social)
    {
        Dictionary<string, object> parametros = new Dictionary<string, object>();
        parametros.Add("CD_PSS", CD_PSS);
        parametros.Add("NOME_SOCIAL", nome_social);

        string sql = @"UPDATE dbZim.dbo.aluf
                       SET nome_social = @nome_social
                       WHERE CD_PSS= @CD_PSS";

        acessoBD.executar(sql, parametros);
    }

    private void SalvarNomeSocialNoZIM(decimal CD_PSS, string nome_social)
    {
        ZimDAL zd = new ZimDAL();

        string sql = $@"UPDATE aluf SET nome_social= '{nome_social}' WHERE cd_pss= '{CD_PSS}'";

        zd.comandoSQLZim(sql);
    }

    private void SalvarPessoaNoSI(T_PSS pessoa)
    {
        PessoaFisica pessoaSI = integracaoMatriculaSI.ObterPessoaSI(pessoa.NR_CPF);
        if (pessoaSI == null)
            pessoaSI = new PessoaFisica();

        pessoaSI.Nome = pessoa.NM_PSS;
        pessoaSI.Mae = pessoa.NM_MAE;
        pessoaSI.Pai = pessoa.NM_PAI;
        pessoaSI.Genero = pessoa.ID_SXE;
        pessoaSI.DataNascimento = pessoa.DT_NSC;

        bool idRacaNaoExisteNoBancoPortal = pessoa.ID_COR_RACA.HasValue == false ||
                                            pessoa.ID_COR_RACA.Value == ID_RACA_PESSOA_NAO_QUIS_DECLARAR ||
                                            pessoa.ID_COR_RACA.Value == ID_RACA_NAO_INFORMADA;
        pessoaSI.RacaId = idRacaNaoExisteNoBancoPortal ? null : pessoa.ID_COR_RACA;

        int estadoCivilId = pessoa.ID_ESTCVL.TrimStart('0').ToInt32();
        pessoaSI.EstadoCivilId = estadoCivilId != 0 ? estadoCivilId : default(int?);

        if (pessoa.CD_NATURALIDADE.Value == (int)ETipoNaturalidade.Brasileiro)
        {
            pessoaSI.EstadoNascimento = ddlDadosAcadUF.SelectedItem.Text.ToUpper();
            pessoaSI.CidadeNascimento = ddlDadosAcadCidade.SelectedItem.Text.Trim().ToUpper();
            pessoaSI.NaturalidadeId = servicoPeriodoLetivo.ObterSICidadeId(ddlDadosAcadCidade.SelectedItem.Value.Trim());
            pessoaSI.PaisNascimentoId = PAIS_BRASIL_ID_SI;
        }
        else if (pessoa.CD_NATURALIDADE.Value == (int)ETipoNaturalidade.Naturalizado)
        {
            pessoaSI.EstadoNascimento = pessoa.NM_ESTADONASCESTR;
            pessoaSI.CidadeNascimento = pessoa.NM_CIDADENASCESTR;
        }

        string dddTelefone = string.IsNullOrWhiteSpace(txtTelefoneResidencialDDD.Text) ? string.Empty : '(' + txtTelefoneResidencialDDD.Text + ") ";
        pessoaSI.Telefone = string.IsNullOrWhiteSpace(txtTelefoneResidencialNumero.Text) ? null : dddTelefone + txtTelefoneResidencialNumero.Text;

        string dddCelular = string.IsNullOrWhiteSpace(txtTelefoneCelularDDD.Text) ? string.Empty : '(' + txtTelefoneCelularDDD.Text + ") ";
        pessoaSI.Celular = string.IsNullOrWhiteSpace(txtTelefoneCelularNumero.Text) ? null : dddCelular + txtTelefoneCelularNumero.Text;

        pessoaSI.Email = txtEmailPrincipal.Text;
        pessoaSI.CPF = Util.CPF.Formatar(pessoa.NR_CPF);
        pessoaSI.RG = pessoa.NR_CI;
        pessoaSI.OrgaoExpedidor_UF = pessoa.NM_ORGEXPCI + "/" + pessoa.SG_ESTCI;
        pessoaSI.DataExpedicao = pessoa.DT_EXPCI;
        pessoaSI.TituloEleitor = pessoa.NR_TTLELT.HasValue ? pessoa.NR_TTLELT.Value.ToString() : null;
        pessoaSI.ZonaTituloEleitor = pessoa.NR_ZNOTTL.HasValue ? pessoa.NR_ZNOTTL.Value.ToString().ToInt32() : default(int?);
        pessoaSI.CidadeTituloEleitorId = pessoa.CD_MNCTTL.HasValue ? servicoPeriodoLetivo.ObterSICidadeId(pessoa.CD_MNCTTL.Value.ToString()) : null;
        pessoaSI.Logradouro = txtEnderecoResidencialLogradouro.Text;
        pessoaSI.Numero = txtEnderecoResidencialNumero.Text;
        pessoaSI.Bairro = txtEnderecoResidencialBairro.Text;
        pessoaSI.Cep = txtEnderecoResidencialCEP.Text.Trim();
        pessoaSI.CidadeId = servicoPeriodoLetivo.ObterSICidadeId(ddlEnderecoResidencialCidade.SelectedValue.Trim());
        pessoaSI.TipoPessoaId = pessoa.CD_NATURALIDADE.Value;

        integracaoMatriculaSI.SalvarPessoaCadastroBasicoNoBancoPortal(pessoaSI);
    }

    protected void trataMatHistoricoAcademico(T_PSS pessoa, decimal RA)
    {
        try
        {
            #region Cria Histal3GR para acadêmicos da FUCMAT ou de Corumbá
            T_ACD acd = pessoa.T_ACD
                              .Select(o => o as T_ACD)
                              .Where(o => (o.CD_PSS == pessoa.CD_PSS &&
                                           o.NR_ACD == RA)).FirstOrDefault();

            if (acd != null)
            {
                    PessoaDadosComplementares dados = pessoa.PessoaDadosComplementares
                                                            .Select(o => o as PessoaDadosComplementares)
                                                            .Where(d => (d.NR_ACD == RA)).FirstOrDefault();
                    string curso = dados.ID_Curso;
                    if (!string.IsNullOrEmpty(curso))
                    {
                        if(acd.ID_FRMING.Equals("F"))
                            new CadastroBasico_SRV().insereZIMHistal3grFUCMAT(acd.NR_ACD.ToString("000000"), curso, Configuracoes.UsuarioZIM);
                        else if (acd.ID_FRMING.Equals("C"))
                        {
                            new CadastroBasico_SRV().insereZIMHistal3grCorumba(acd.NR_ACD.ToString("000000"), curso, Configuracoes.UsuarioZIM);
                        return;
                        }
                    }
            }
            #endregion

            MatHistoricoAcademico historico = pessoa.MatHistoricoAcademico
                                                    .Select(o => o as MatHistoricoAcademico)
                                                    .Where(o => (o.CD_PSSACD == pessoa.CD_PSS &&
                                                                 o.NR_ACD == RA)).FirstOrDefault();

            if (historico == null)
            {
                // Há cadastros antigos que não possuem MatHistoricoAcademico. Há uma tentativa de recuperar informações antigas (utilizando a procedure) caso existam.
                ZIM.PST.HistAl3GrPST HistAl = new ZIM.PST.HistAl3GrPST();
                if (HistAl.listaHistAl3GrPorc_ident_al(RA.ToString("000000")).Count > 0)
                {
                    StringWriter sw = new StringWriter();
                    Server.Execute(string.Format("~/Z/FR_Atualizar.aspx?t=histal3gr&RA={0}&horas=0", RA.ToString("000000")), sw, false);

                    string sqlprocCon = "PR_IMPORTAHISTZIM '" + RA.ToString("000000") + "'";
                    DAL.Acesso acs = new Acesso();
                    acs.executar(sqlprocCon);
                }

                // Se a procedure não encontrou informações antigas para preencher o mathistoricoacademico, é adicionado o primeiro registro do academico.
                MatHistoricoAcademico registroHistorico = servico.getMatHistoricoAcademico(RA);

                if (registroHistorico == null)
                {
                    GerencialPeriodoLetivo_SRV gerencial = new GerencialPeriodoLetivo_SRV();
                    string ID_Grade = gerencial.getGradeCursoPorPL(ddlCursos.SelectedValue, Configuracoes.PL_Matricula, 1);
                    int ID_Semestre = servico.getSemestreOferecido(ID_Grade, ddlCursos.SelectedValue, Configuracoes.PL_Matricula);

                    historico = new MatHistoricoAcademico();
                    historico.NR_ACD = RA;
                    historico.ID_Curso = ddlCursos.SelectedValue.Trim().ToUpper();
                    historico.ID_Grade = acd != null && acd.ID_FRMING.Equals("F") ? "000" : ID_Grade.ToUpper();
                    historico.UltPeriodoLetivoMatriculado = Configuracoes.PL_Matricula;
                    historico.UltSemRegularCursado = ID_Semestre > 0 ? ID_Semestre : 1;
                    historico.AnoInicioCurso = Configuracoes.PL_Matricula;
                    historico.CD_PSS = Convert.ToDecimal(Configuracoes.Pessoa);
                    historico.CD_PSSACD = pessoa.CD_PSS;
                    historico.DataHistorico = DateTime.Now;
                    historico.ID_historicoOrigem = null;
                    historico.ID_TipoMovimentoHistorico = (int)UCDB_SRV.ObjetosAuxiliares_SRV.Enuns.enumTipoMovimentoHistorico.CADASTRO;

                    servico.SalvarMatHistoricoAcademico(historico);
                }
            }
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    protected string criptografaDados(string d)
    {
        if (!string.IsNullOrEmpty(d))
        {
            byte[] bytes = ASCIIEncoding.ASCII.GetBytes(d);
            return Convert.ToBase64String(bytes);
        }

        return string.Empty;
    }

    protected bool ValidaInformacoes()
    {
        if (ddlCursos.SelectedItem.Value.Equals("0"))
        {
            windowManager.RadAlert("Selecione o curso!", 350, 110, "ATENÇÃO!", null);
            return false;
        }

        if (ddlPolo.SelectedItem.Value.Equals("0"))
        {
            windowManager.RadAlert("Selecione o polo!", 350, 110, "ATENÇÃO!", null);
            return false;
        }

        //rblNaturalidade

        if (ckbNomeSocial.Checked == true && string.IsNullOrEmpty(txtNomeSocial.Text))
        {
            windowManager.RadAlert("Informe o Nome Social do Acadêmico!", 350, 110, "ATENÇÃO!", null);
            return false;
        }

        if (string.IsNullOrEmpty(txtDadosAcadNome.Text.Trim()))
        {
            windowManager.RadAlert("Informe o Nome do Acadêmico!", 350, 110, "ATENÇÃO!", null);
            return false;
        }

        // txtDadosAcadNomeMae // está sendo validado no 'Resp. Financeiro'
        // txtDadosAcadNomePai // está sendo validado no 'Resp. Financeiro'

        if (rblDadosAcadSexo.SelectedIndex == -1)
        {
            windowManager.RadAlert("Informe o Sexo!", 350, 110, "ATENÇÃO!", null);
            return false;
        }

        // Foi utilizado o valor "-1" para o SELECIONE, pois o valor "0" já está sendo utilizado para um registro.
        if (ddlDadosAcadEstCivil.SelectedItem.Value == "-1")
        {
            windowManager.RadAlert("Informe o Estado Civil!", 350, 110, "ATENÇÃO!", null);
            return false;
        }

        if (dpDadosAcadNascimento.IsEmpty)
        {
            windowManager.RadAlert("Informe a Data de Nascimento!", 350, 110, "ATENÇÃO!", null);
            return false;
        }

        try
        {
            if (dpDadosAcadNascimento.SelectedDate >= DateTime.Now)
            {
                windowManager.RadAlert("Data de Nascimento inválida!", 350, 110, "ATENÇÃO!", null);
                return false;
            }
        }
        catch (Exception)
        {
            windowManager.RadAlert("Data de Nascimento está com formato inválido!", 350, 110, "ATENÇÃO!", null);
            return false;
        }

        if ((ETipoNaturalidade)Convert.ToInt32(rblNaturalidade.SelectedItem.Value) == ETipoNaturalidade.Estrangeiro)
        {
            if (ddlDadosAcadNacionalidade.SelectedItem.Value == "0")
            {
                windowManager.RadAlert("Informe a Nacionalidade!", 350, 110, "ATENÇÃO!", null);
                return false;
            }
        }

        if ((ETipoNaturalidade)Convert.ToInt32(rblNaturalidade.SelectedItem.Value) == ETipoNaturalidade.Brasileiro
            && !chkRegistrado_Consulado.Checked)
        {
            if (ddlDadosAcadUF.SelectedItem.Value == "0")
            {
                windowManager.RadAlert("Informe a UF de nascimento!", 350, 110, "ATENÇÃO!", null);
                return false;
            }
            if (ddlDadosAcadCidade.SelectedItem.Value == "0")
            {
                windowManager.RadAlert("Informe a Cidade de nascimento!", 350, 110, "ATENÇÃO!", null);
                return false;
            }
        }
        else
        {
            if (((ETipoNaturalidade)Convert.ToInt32(rblNaturalidade.SelectedItem.Value) == ETipoNaturalidade.Naturalizado) ||
                ((ETipoNaturalidade)Convert.ToInt32(rblNaturalidade.SelectedItem.Value) == ETipoNaturalidade.Estrangeiro) ||
                chkRegistrado_Consulado.Checked)
            {
                if (string.IsNullOrEmpty(txtDadosAcadNAOBrEstado.Text.Trim()))
                {
                    windowManager.RadAlert("Informe o Estado/Distrito de nascimento!", 350, 110, "ATENÇÃO!", null);
                    return false;
                }
                if (string.IsNullOrEmpty(txtDadosAcadNAOBrCidade.Text.Trim()))
                {
                    windowManager.RadAlert("Informe a Cidade de nascimento!", 350, 110, "ATENÇÃO!", null);
                    return false;
                }
            }
        }

        ///////////////////////////////
        //ddlDadosAcadRespFinanceiro //
        ///////////////////////////////
        // Text="O mesmo"  Value="A  //
        // Text="Mãe"      Value="M" //
        // Text="Pai"      Value="P" //
        // Text="Outro"    Value="O" //
        ///////////////////////////////
        switch (ddlDadosAcadRespFinanceiro.SelectedItem.Value)
        {
            case "A": // O nome do acadêmico já é obrigatório.
                break;

            case "M":
                if (string.IsNullOrEmpty(txtDadosAcadNomeMae.Text.Trim()))
                {
                    windowManager.RadAlert("Foi selecionado a Mãe como responsável financeiro.<br>Informe o Nome da Mãe!", 450, 110, "ATENÇÃO!", null);
                    return false;
                }
                if ((ETipoNaturalidade)Convert.ToInt32(rblNaturalidade.SelectedItem.Value) != ETipoNaturalidade.Estrangeiro)
                {
                    if (string.IsNullOrEmpty(txtDocumentosCPFMae.Text.Trim()))
                    {
                        windowManager.RadAlert("Foi selecionado a Mãe como responsável financeiro.<br>Informe o CPF da Mãe!", 450, 110, "ATENÇÃO!", null);
                        return false;
                    }
                    else if (!funcoes.isCPFValido(txtDocumentosCPFMae.Text.Trim()))
                    {
                        windowManager.RadAlert("CPF da Mãe está inválido!", 350, 110, "ATENÇÃO!", null);
                        return false;
                    }
                }
                break;

            case "P":
                if (string.IsNullOrEmpty(txtDadosAcadNomePai.Text.Trim()))
                {
                    windowManager.RadAlert("Foi selecionado o Pai como responsável financeiro.<br>Informe o Nome do Pai!", 450, 110, "ATENÇÃO!", null);
                    return false;
                }
                if ((ETipoNaturalidade)Convert.ToInt32(rblNaturalidade.SelectedItem.Value) != ETipoNaturalidade.Estrangeiro)
                {
                    if (string.IsNullOrEmpty(txtDocumentosCPFPai.Text.Trim()))
                    {
                        windowManager.RadAlert("Foi selecionado o Pai como responsável financeiro.<br>Informe o CPF do Pai!", 450, 110, "ATENÇÃO!", null);
                        return false;
                    }
                    else if (!funcoes.isCPFValido(txtDocumentosCPFPai.Text.Trim()))
                    {
                        windowManager.RadAlert("CPF do Pai está inválido!", 350, 110, "ATENÇÃO!", null);
                        return false;
                    }
                }
                break;

            case "O":
                if (string.IsNullOrEmpty(txtDadosAcadNomeResponsavel.Text.Trim()))
                {
                    windowManager.RadAlert("Informe o Nome do Resp.!", 350, 110, "ATENÇÃO!", null);
                    return false;
                }
                if ((ETipoNaturalidade)Convert.ToInt32(rblNaturalidade.SelectedItem.Value) != ETipoNaturalidade.Estrangeiro)
                {
                    if (string.IsNullOrEmpty(txtDocumentosCPFResp.Text.Trim()))
                    {
                        windowManager.RadAlert("Foi selecionado OUTRO como responsável financeiro.<br>Informe o CPF do Responsável!", 450, 110, "ATENÇÃO!", null);
                        return false;
                    }
                    else if (!funcoes.isCPFValido(txtDocumentosCPFResp.Text.Trim()))
                    {
                        windowManager.RadAlert("CPF do Resp. Financeiro está inválido!", 350, 110, "ATENÇÃO!", null);
                        return false;
                    }
                }
                break;
        }

        if (ddlDadosAcadFormaIngresso.SelectedItem.Value == "0")
        {
            windowManager.RadAlert("Informe a Forma de Ingresso!", 350, 110, "ATENÇÃO!", null);
            return false;
        }

        if (string.IsNullOrEmpty(txtEmailPrincipal.Text))
        {
            windowManager.RadAlert("Informe o E-mail Principal!", 350, 110, "ATENÇÃO!", null);
            return false;
        }






        #region Residencial obrigatorio... antigo

        //if (string.IsNullOrEmpty(txtTelefoneResidencialDDD.Text.Trim()))
        //{
        //    windowManager.RadAlert("Informe o DDD do Telefone Residencial!", 350, 110, "ATENÇÃO!", null);
        //    return false;
        //}
        //else
        //{
        //    if (txtTelefoneResidencialDDD.Text.Trim().Length != 2)
        //    {
        //        windowManager.RadAlert("O DDD do Telefone Residencial está incompleto!", 350, 110, "ATENÇÃO!", null);
        //        return false;
        //    }
        //}

        //if (string.IsNullOrEmpty(txtTelefoneResidencialNumero.Text.Trim()))
        //{
        //    windowManager.RadAlert("Informe o Número do Telefone Residencial!", 350, 110, "ATENÇÃO!", null);
        //    return false;
        //}
        //else
        //{
        //    // pode ter 8 ou 9 digitos
        //    //if (!(Convert.ToDecimal(txtTelefoneResidencialNumero.Text).ToString().Length == 9 || Convert.ToDecimal(txtTelefoneResidencialNumero.Text).ToString().Length == 8))

        //    if (Convert.ToDecimal(txtTelefoneResidencialNumero.Text).ToString().Length != 8)
        //    {
        //        windowManager.RadAlert("O Número do Telefone Residencial está incompleto!<br>Deve conter 8 dígitos!", 450, 110, "ATENÇÃO!", null);
        //        return false;
        //    }
        //}
        #endregion







        ////////////////////////////////////////////////////////////////
        //                                                            //
        //   XOR    = (p ^ ¬ q) V (¬p ^q)                             //
        //   string.IsNullOrEmpty( txtTelefoneCelularDDD.Text)    p   //
        //   string.IsNullOrEmpty(txtTelefoneCelularNumero.Text)  q   //
        //                                                            //
        ////////////////////////////////////////////////////////////////
        if ((string.IsNullOrEmpty(txtTelefoneResidencialDDD.Text) && !string.IsNullOrEmpty(txtTelefoneResidencialNumero.Text)) ||
            (!string.IsNullOrEmpty(txtTelefoneResidencialDDD.Text) && string.IsNullOrEmpty(txtTelefoneResidencialNumero.Text)))
        {
            windowManager.RadAlert("Telefone Residencial está incompleto!", 350, 110, "ATENÇÃO!", null);
            return false;
        }

        if (!string.IsNullOrEmpty(txtTelefoneResidencialDDD.Text) && !string.IsNullOrEmpty(txtTelefoneResidencialNumero.Text))
        {
            if (txtTelefoneResidencialDDD.Text.Trim().Length != 2)
            {
                windowManager.RadAlert("O DDD do Telefone Residencial <br>Deve conter 2 dígitos!", 350, 110, "ATENÇÃO!", null);
                return false;
            }

            if (Convert.ToDecimal(txtTelefoneResidencialNumero.Text).ToString().Length < 8 || Convert.ToDecimal(txtTelefoneResidencialNumero.Text).ToString().Length > 9)
            {
                windowManager.RadAlert("O Número do Telefone Residencial <br>Deve conter 8 ou 9 dígitos!", 450, 110, "ATENÇÃO!", null);
                return false;
            }
        }

        ////////////////////////////////////////////////////////////////
        //                                                            //
        //   XOR    = (p ^ ¬ q) V (¬p ^q)                             //
        //   string.IsNullOrEmpty( txtTelefoneCelularDDD.Text)    p   //
        //   string.IsNullOrEmpty(txtTelefoneCelularNumero.Text)  q   //
        //                                                            //
        ////////////////////////////////////////////////////////////////
        if ((string.IsNullOrEmpty(txtTelefoneCelularDDD.Text) && !string.IsNullOrEmpty(txtTelefoneCelularNumero.Text)) ||
            (!string.IsNullOrEmpty(txtTelefoneCelularDDD.Text) && string.IsNullOrEmpty(txtTelefoneCelularNumero.Text)))
        {
            windowManager.RadAlert("Telefone Celular está incompleto!", 350, 110, "ATENÇÃO!", null);
            return false;
        }

        if (!string.IsNullOrEmpty(txtTelefoneCelularDDD.Text) && !string.IsNullOrEmpty(txtTelefoneCelularNumero.Text))
        {
            if (Convert.ToDecimal(txtTelefoneCelularDDD.Text).ToString().Length != 2)
            {
                windowManager.RadAlert("O DDD do Telefone Celular <br>Deve conter 2 dígitos!", 450, 110, "ATENÇÃO!", null);
                return false;
            }

            if (Convert.ToDecimal(txtTelefoneCelularNumero.Text).ToString().Length < 8 || Convert.ToDecimal(txtTelefoneCelularNumero.Text).ToString().Length > 9)
            {
                windowManager.RadAlert("O Número do Telefone Celular <br>Deve conter 8 ou 9 dígitos!", 450, 110, "ATENÇÃO!", null);
                return false;
            }
        }

        if (string.IsNullOrEmpty(txtTelefoneCelularDDD.Text.Trim()) && string.IsNullOrEmpty(txtTelefoneCelularNumero.Text.Trim()) &&
            string.IsNullOrEmpty(txtTelefoneResidencialDDD.Text.Trim()) && string.IsNullOrEmpty(txtTelefoneResidencialNumero.Text.Trim()))
        {
            windowManager.RadAlert("Atenção! Um dos campos do telefone deve ser preenchido!", 350, 110, "ATENÇÃO!", null);
            return false;
        }

        Regex rg = new Regex(@"^[A-Za-z0-9]((([_\-]*)[.]?[a-zA-Z0-9]+)*)([_\-]*)@([A-Za-z0-9]+)(([\.\-]?[a-zA-Z0-9]+)*)\.([A-Za-z]{2,})$");
        if (!string.IsNullOrEmpty(txtEmailPrincipal.Text.Trim()))
        {
            if (!rg.IsMatch(txtEmailPrincipal.Text.Trim()))
            {
                windowManager.RadAlert("E-mail Principal está em um formato inválido!", 350, 110, "ATENÇÃO!", null);
                return false;
            }
        }

        if (!string.IsNullOrEmpty(txtEmailAlternativo.Text.Trim()))
        {
            if (!rg.IsMatch(txtEmailAlternativo.Text.Trim()))
            {
                windowManager.RadAlert("E-mail Alternativo está em um formato inválido!", 350, 110, "ATENÇÃO!", null);
                return false;
            }
        }

        if (((ETipoNaturalidade)Convert.ToInt32(rblNaturalidade.SelectedItem.Value) == ETipoNaturalidade.Brasileiro) ||
            ((ETipoNaturalidade)Convert.ToInt32(rblNaturalidade.SelectedItem.Value) == ETipoNaturalidade.Naturalizado))
        {
            if (string.IsNullOrEmpty(txtDocumentosCPF.Text.Trim()))
            {
                windowManager.RadAlert("Informe o CPF do Acadêmico!", 350, 110, "ATENÇÃO!", null);
                return false;
            }

            if (!funcoes.isCPFValido(txtDocumentosCPF.Text.Trim()))
            {
                windowManager.RadAlert("CPF do Acadêmico está inválido!", 350, 110, "ATENÇÃO!", null);
                return false;
            }

            // Anderson da secretaria academica falou para deixar livre para preenchimento. ( tudo ).
            if (string.IsNullOrEmpty(txtDocumentosRG.Text.Trim()))
            {
                windowManager.RadAlert("Informe o RG!", 350, 110, "ATENÇÃO!", null);
                return false;
            }

            if (string.IsNullOrEmpty(txtDocumentosRGOrgEmissor.Text.Trim()))
            {
                windowManager.RadAlert("Informe Órgão Emissor do RG!", 350, 110, "ATENÇÃO!", null);
                return false;
            }

            if (ddlDocumentosRGOrgEmissorUF.SelectedItem.Value == "0")
            {
                windowManager.RadAlert("Informe a UF do Órgão Emissor do RG!", 350, 110, "ATENÇÃO!", null);
                return false;
            }

            if (dpDocumentosRGDataExpedicao.IsEmpty)
            {
                windowManager.RadAlert("Informe a Data de Expedição do RG!", 350, 110, "ATENÇÃO!", null);
                return false;
            }

            if (dpDocumentosRGDataExpedicao.SelectedDate >= DateTime.Now)
            {
                windowManager.RadAlert("Data de Expedição do RG inválida!", 350, 110, "ATENÇÃO!", null);
                return false;
            }

            // se o titulo estiver preenchido, quer dizer que as outras informações estarão preenchidas.
            if (!string.IsNullOrEmpty(txtDocumentosTituloEleitorNumero.Text.Trim()))
            {
                if (txtDocumentosTituloEleitorNumero.Text.Trim().Length != 12)
                {
                    windowManager.RadAlert("Número do título do eleitor deve conter 12 digitos!", 350, 110, "ATENÇÃO!", null);
                    return false;
                }

                if (txtDocumentosTituloEleitorZona.Text.Trim().Length != 4)
                {
                    windowManager.RadAlert("Zona do título do eleitor deve conter 4 digitos!", 350, 110, "ATENÇÃO!", null);
                    return false;
                }

                if (ddlDocumentosTituloEleitorUF.SelectedItem.Value == "0")
                {
                    windowManager.RadAlert("Informe a UF do Título de Eleitor!", 350, 110, "ATENÇÃO!", null);
                    return false;
                }

                if (ddlDocumentosTituloEleitorCidade.SelectedItem.Value == "0")
                {
                    windowManager.RadAlert("Informe a Cidade do Título de Eleitor!", 350, 110, "ATENÇÃO!", null);
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(txtDocumentosMilitarNumero.Text.Trim()))
            {
                if (string.IsNullOrEmpty(txtDocumentosMilitarSerie.Text.Trim()))
                {
                    windowManager.RadAlert("Informe a Série do Documento Militar!", 350, 110, "ATENÇÃO!", null);
                    return false;
                }

                //txtDocumentosMilitarComplemento

                if (string.IsNullOrEmpty(txtDocumentosMilitarSituacao.Text))
                {
                    windowManager.RadAlert("Informe a Situação do Documento Militar!", 350, 110, "ATENÇÃO!", null);
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(txtDocumentosCPFMae.Text.Trim()))
            {
                if (!funcoes.isCPFValido(txtDocumentosCPFMae.Text.Trim()))
                {
                    windowManager.RadAlert("CPF da Mãe está inválido!", 350, 110, "ATENÇÃO!", null);
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(txtDocumentosCPFPai.Text.Trim()))
            {
                if (!funcoes.isCPFValido(txtDocumentosCPFPai.Text.Trim()))
                {
                    windowManager.RadAlert("CPF do Pai está inválido!", 350, 110, "ATENÇÃO!", null);
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(txtDocumentosCPFResp.Text.Trim()))
            {
                if (!funcoes.isCPFValido(txtDocumentosCPFResp.Text.Trim()))
                {
                    windowManager.RadAlert("CPF do Resp. inválido!", 350, 110, "ATENÇÃO!", null);
                    return false;
                }
            }
        }
        else
        {
            if ((ETipoNaturalidade)Convert.ToInt32(rblNaturalidade.SelectedItem.Value) == ETipoNaturalidade.Estrangeiro)
            {
                ///////////////////////////////////////////////////////////////////////////////////////////////////////////
                ///////////////////////////////////////////////////////////////////////////////////////////////////////////
                ///
                /// temporariamente
                /// 
                ///////////////////////////////////////////////////////////////////////////////////////////////////////////
                ///////////////////////////////////////////////////////////////////////////////////////////////////////////
                // Não é obrigatório, porém, se algum for preenchido deve ser preenchido por completo.
                if (!string.IsNullOrEmpty(txtDocumentosPassaporteNumero.Text.Trim()) ||
                    !string.IsNullOrEmpty(txtDocumentosPassaporteNumero.Text.Trim()) ||
                    !dpDocumentosPassaporteDataEmissao.IsEmpty ||
                    !dpDocumentosPassaporteDataValidade.IsEmpty ||
                    !string.IsNullOrEmpty(txtDocumentosPassaportePais.Text.Trim()))
                {
                    if (string.IsNullOrEmpty(txtDocumentosPassaporteNumero.Text.Trim()))
                    {
                        windowManager.RadAlert("Informe Nº do Passaporte!", 350, 110, "ATENÇÃO!", null);
                        return false;
                    }

                    if (dpDocumentosPassaporteDataEmissao.IsEmpty)
                    {
                        windowManager.RadAlert("Informe a Data de Emissão do Passaporte!", 350, 110, "ATENÇÃO!", null);
                        return false;
                    }

                    if (dpDocumentosPassaporteDataValidade.IsEmpty)
                    {
                        windowManager.RadAlert("Informe a Data de Validade do Passaporte!", 350, 110, "ATENÇÃO!", null);
                        return false;
                    }

                    if (string.IsNullOrEmpty(txtDocumentosPassaportePais.Text.Trim()))
                    {
                        windowManager.RadAlert("Informe o País de Emissão do Passaporte!", 350, 110, "ATENÇÃO!", null);
                        return false;
                    }

                    //txtDocumentosCPFEstrangeiro
                }
                else //////////////////////////////////////////////////////////////////////////////////////////////////////////////////// temporario
                {
                    windowManager.RadAlert("Atenção! Os dados do passaporte não são obrigatórios, porém,<br>pedimos que essas informações sejam solicitadas ao acadêmico para atualização do cadastro!", 650, 110, "ATENÇÃO!", null);
                }

                if (!string.IsNullOrEmpty(txtDocumentosRNE.Text.Trim()))
                {
                    if (string.IsNullOrEmpty(txtDocumentosRNEOrgEmissor.Text.Trim())) // Fica obrigatório caso o número RNE for preenchido.
                    {
                        windowManager.RadAlert("Informe o Órgão Emissor do RNE!", 350, 110, "ATENÇÃO!", null);
                        return false;
                    }
                }
            }
        }

        #region Endereço Residencial

        if (string.IsNullOrEmpty(txtEnderecoResidencialLogradouro.Text.Trim()))
        {
            windowManager.RadAlert("Informe o Logradouro do Endereço Residencial!", 350, 110, "ATENÇÃO!", null);
            return false;
        }

        // TEM CASOS COMO DISTRITO QUE AS CASAS SAO SEPARADAS EM LOTES
        //if (string.IsNullOrEmpty(txtEnderecoResidencialNumero.Text.Trim()))
        //{
        //    windowManager.RadAlert("Informe o Número do Endereço Residencial!", 350, 110, "ATENÇÃO!", null);
        //    return false;
        //}        
        //else 
        //{
        //    if (Convert.ToInt32(txtEnderecoResidencialNumero.Text) == 0)
        //    {
        //        windowManager.RadAlert("Informe o Número do Endereço Residencial!", 350, 110, "ATENÇÃO!", null);
        //        return false;
        //    }
        //}

        //txtEnderecoResidencialComplemento

        if (string.IsNullOrEmpty(txtEnderecoResidencialBairro.Text.Trim()))
        {
            windowManager.RadAlert("Informe o Bairro do Endereço Residencial!", 350, 110, "ATENÇÃO!", null);
            return false;
        }

        if (string.IsNullOrEmpty(txtEnderecoResidencialCEP.Text.Trim()))
        {
            windowManager.RadAlert("Informe o CEP do Endereço Residencial!", 350, 110, "ATENÇÃO!", null);
            return false;
        }
        else
        {
            if (Convert.ToInt32(txtEnderecoResidencialCEP.Text) == 0)
            {
                windowManager.RadAlert("Informe o CEP do Endereço Residencial!", 350, 110, "ATENÇÃO!", null);
                return false;
            }

            if (txtEnderecoResidencialCEP.Text.Trim().Length != 8)
            {
                windowManager.RadAlert("CEP do Endereço Residencial inválido!", 350, 110, "ATENÇÃO!", null);
                return false;
            }
        }

        if (ddlEnderecoResidencialUF.SelectedItem.Value == "0")
        {
            windowManager.RadAlert("Informe a UF do Endereço Residencial!", 350, 110, "ATENÇÃO!", null);
            return false;
        }

        if (ddlEnderecoResidencialCidade.SelectedItem.Value == "0")
        {
            windowManager.RadAlert("Informe a Cidade do Endereço Residencial!", 350, 110, "ATENÇÃO!", null);
            return false;
        }
        #endregion

        // Endereço Correspondência não é obrigatório, porém, se for preenchido deve ser completo
        if (rblEnderecoTipoCorrespondencia.SelectedItem.Value == Convert.ToInt32(ETipoEndereço.No_Brasil).ToString())
        {
            // Se existir pelo menos um preenchido, deve ser completo o cadastro desse endereço
            // Verifica se tem pelo menos 1 preenchido
            if (!string.IsNullOrEmpty(txtEnderecoCorrespondenciaLogradouro.Text) ||
                !string.IsNullOrEmpty(txtEnderecoCorrespondenciaNumero.Text) ||
                !string.IsNullOrEmpty(txtEnderecoCorrespondenciaComplemento.Text) ||
                !string.IsNullOrEmpty(txtEnderecoCorrespondenciaBairro.Text) ||
                !string.IsNullOrEmpty(txtEnderecoCorrespondenciaCEP.Text) ||
                ddlEnderecoCorrespondenciaUF.SelectedItem.Value != "0" ||
                ddlEnderecoCorrespondenciaCidade.SelectedItem.Value != "0")
            {
                // Verifica se tem algum dos obrigatórios em branco
                if (string.IsNullOrEmpty(txtEnderecoCorrespondenciaLogradouro.Text) ||
                    //string.IsNullOrEmpty(txtEnderecoCorrespondenciaNumero.Text) || => TEM CASAS QUE NAO TEM NUMERO
                    //string.IsNullOrEmpty(txtEnderecoCorrespondenciaComplemento.Text) || => 'Complemento' não é obrigatório
                    string.IsNullOrEmpty(txtEnderecoCorrespondenciaBairro.Text) ||
                    string.IsNullOrEmpty(txtEnderecoCorrespondenciaCEP.Text) ||
                    ddlEnderecoCorrespondenciaUF.SelectedItem.Value == "0" ||
                    ddlEnderecoCorrespondenciaCidade.SelectedItem.Value == "0")
                {
                    windowManager.RadAlert("O Endereço Correspondência não é obrigatório, porém,<br>se algum campo for preenchido deve ser preenchido por completo!<br>Complete o Endereço Correspondência!", 450, 110, "ATENÇÃO!", null);
                    return false;
                }
            }
        }
        else
        {
            // Se existir pelo menos um preenchido, deve ser completo o cadastro desse endereço
            // Verifica se tem pelo menos 1 preenchido
            if (!string.IsNullOrEmpty(txtEnderecoCorrespondenciaFORAEndereco.Text) ||
                !string.IsNullOrEmpty(txtEnderecoCorrespondenciaFORAEstadoDistrito.Text) ||
                !string.IsNullOrEmpty(txtEnderecoCorrespondenciaFORACidade.Text) ||
                !string.IsNullOrEmpty(txtEnderecoCorrespondenciaFORACodigoPostal.Text) ||
                ddlEnderecoCorrespondenciaFORAPais.SelectedItem.Value != "0")
            {
                // Verifica se tem algum dos obrigatórios em branco
                if (string.IsNullOrEmpty(txtEnderecoCorrespondenciaFORAEndereco.Text) ||
                    string.IsNullOrEmpty(txtEnderecoCorrespondenciaFORAEstadoDistrito.Text) ||
                    string.IsNullOrEmpty(txtEnderecoCorrespondenciaFORACidade.Text) ||
                    string.IsNullOrEmpty(txtEnderecoCorrespondenciaFORACodigoPostal.Text) ||
                    ddlEnderecoCorrespondenciaFORAPais.SelectedItem.Value == "0")
                {
                    windowManager.RadAlert("O Endereço Correspondência não é obrigatório, porém,<br>se algum campo for preenchido deve ser preenchido por completo!<br>Complete o Endereço Correspondência!", 450, 110, "ATENÇÃO!", null);
                    return false;
                }
            }
        }

        bool cursoSelecionadoEhGraduacao = servicoPeriodoLetivo.CursoEhDeGraduacao(ddlCursos.SelectedItem.Value);

        if (string.IsNullOrEmpty(txtInformacoesIDEscola.Text) && cursoSelecionadoEhGraduacao)
        {
            windowManager.RadAlert("Informe o código da Escola do Ensino Médio!", 350, 110, "ATENÇÃO!", null);
            return false;
        }

        if (string.IsNullOrEmpty(txtInformacoesEscola.Text.Trim()) && cursoSelecionadoEhGraduacao)
        {
            windowManager.RadAlert("Informe a Escola do Ensino Médio!", 350, 110, "ATENÇÃO!", null);
            return false;
        }

        if (string.IsNullOrEmpty(txtInformacoesEscolaAnoConclusao.Text) && cursoSelecionadoEhGraduacao)
        {
            windowManager.RadAlert("Informe o ano de conclusão do Ensino Médio!", 350, 110, "ATENÇÃO!", null);
            return false;
        }

        if (string.IsNullOrEmpty(txtDadosAcadNome.Text.Trim()))
        {
            windowManager.RadAlert("Informe o Nome do Acadêmico!", 350, 110, "ATENÇÃO!", null);
            return false;
        }
        //txtInformacoesIDIes
        //txtInformacoesIes
        //txtInformacoesAnoVestibular
        //txtInformacoesDiscVest
        //txtInformacoesIDTransferido
        //txtInformacoesTranferido

        return true;
    }

    protected T_PSS CarregaObjetoParaSalvar()
    {
        string[] raNOVO = new string[2];
        T_PSS pessoa;
        T_ACD academico;
        PessoaDadosComplementares academicoPessoaDadosComplementares;
        CadastroBasico_SRV servico = new CadastroBasico_SRV();

        try
        {
            #region Prepara os objetos para receber informação da tela.
            if (!string.IsNullOrEmpty(hfEhCadastro.Value) && hfEhCadastro.Value.Equals("true")) // Cadastro Novo
            {
                pessoa = new T_PSS();
                academico = new T_ACD();
                academicoPessoaDadosComplementares = new PessoaDadosComplementares();

                pessoa.T_ACD.Add(academico);
                pessoa.PessoaDadosComplementares.Add(academicoPessoaDadosComplementares);

                raNOVO = RALivre();
                academico.NR_ACD = Convert.ToDecimal(raNOVO[0]);
                academico.NR_DGTVRF = Convert.ToDecimal(raNOVO[1]);
                hfRA.Value = raNOVO[0]; // Caso não consiga salvar, o RA guardado no hidden é liberado no Exception
            }
            else // Edição
            {
                academico = servico.getAcademico(Convert.ToInt32(hfRA.Value));

                if (academico == null)
                {
                    academico = new T_ACD();
                    raNOVO = RALivre();
                    academico.NR_ACD = Convert.ToDecimal(raNOVO[0]);
                    academico.NR_DGTVRF = Convert.ToDecimal(raNOVO[1]);
                    hfRA.Value = raNOVO[0]; // Caso não consiga salvar, o RA guardado no hidden é liberado no Exception

                    pessoa = servico.getPessoa(hfCPF.Value); // Caso não tenha T_ACD, é verificado se existe um T_PSS já cadastrado pra essa pessoa.

                    if (pessoa == null)
                        pessoa = new T_PSS();

                    pessoa.T_ACD.Add(academico);

                    academicoPessoaDadosComplementares = new PessoaDadosComplementares();
                    pessoa.PessoaDadosComplementares.Add(academicoPessoaDadosComplementares);
                }
                else
                {
                    pessoa = academico.T_PSS;
                    academicoPessoaDadosComplementares = pessoa.PessoaDadosComplementares
                                                               .Select(o => o as PessoaDadosComplementares)
                                                               .Where(o => o.NR_ACD == academico.NR_ACD).FirstOrDefault();

                    if (academicoPessoaDadosComplementares == null)
                    {
                        academicoPessoaDadosComplementares = new PessoaDadosComplementares();
                        pessoa.PessoaDadosComplementares.Add(academicoPessoaDadosComplementares);
                    }
                }
            }
            #endregion

            DateTime dataDaAtualizacao = DateTime.Now;

            // Valores que estavam na página SII/FR_Academico.aspx
            //"A" = Ativo
            //"I" = Inativo
            //"D" = Desistente
            //"C" = Cancelado
            //"F" = Trancado
            //"T" = Transferido
            //"O" = Óbito
            academico.ID_STCACD = string.IsNullOrEmpty(academico.ID_STCACD) ? "A" : academico.ID_STCACD.Trim();

            academicoPessoaDadosComplementares.NR_ACD = academico.NR_ACD;

            academicoPessoaDadosComplementares.ID_Curso = ddlCursos.SelectedValue.Trim();
            academicoPessoaDadosComplementares.ID_polo = Convert.ToInt32(ddlPolo.SelectedValue);

            academico.NM_ACD = txtDadosAcadNome.Text.Trim().ToUpper();
            pessoa.NM_PSS = txtDadosAcadNome.Text.Trim().ToUpper();
            pessoa.NM_ABR = txtDadosAcadNome.Text.Trim().Length >= 150 ? txtDadosAcadNome.Text.Trim().Substring(0, 150) : txtDadosAcadNome.Text.Trim().ToUpper();

            pessoa.NM_MAE = txtDadosAcadNomeMae.Text.Trim().ToUpper();
            pessoa.NM_PAI = txtDadosAcadNomePai.Text.Trim().ToUpper();
            pessoa.ID_SXE = rblDadosAcadSexo.SelectedValue;
            pessoa.ID_ESTCVL = ddlDadosAcadEstCivil.SelectedValue.PadLeft(2, '0');
            pessoa.ID_COR_RACA = Convert.ToInt32(ddlDadosAcadCorRaca.SelectedValue);

            pessoa.DT_NSC = dpDadosAcadNascimento.SelectedDate;

            if (chkRegistrado_Consulado.Checked)
            {
                pessoa.BL_REGCONSULADO = 1;
                pessoa.NM_ESTADONASCESTR = txtDadosAcadNAOBrEstado.Text.Trim().ToUpper();
                pessoa.NM_CIDADENASCESTR = txtDadosAcadNAOBrCidade.Text.Trim().ToUpper();
                ddlDadosAcadUF.SelectedValue = null;
                ddlDadosAcadCidade.SelectedValue = null;
            }
            else
            {
                pessoa.BL_REGCONSULADO = null;
                pessoa.NM_ESTADONASCESTR = null;
                pessoa.NM_CIDADENASCESTR = null;
            }

            pessoa.CD_NATURALIDADE = Convert.ToInt32(rblNaturalidade.SelectedItem.Value);

            if ((ETipoNaturalidade)Convert.ToInt32(rblNaturalidade.SelectedItem.Value) == ETipoNaturalidade.Estrangeiro)
            {
                pessoa.DS_NCN = ddlDadosAcadNacionalidade.SelectedItem.Text.Trim().ToUpper();
                pessoa.CD_PSANSC = Convert.ToDecimal(ddlDadosAcadNacionalidade.SelectedItem.Value);
            }
            else
            {
                //lblDadosAcadNacionalidade
                pessoa.DS_NCN = "BRASILEIRA"; // dbZIM.dbo.PaisNacion // n_nacion
                pessoa.CD_PSANSC = 10;        // dbZIM.dbo.PaisNacion // n_nacion
            }

            if (trCidadeEstadoBrasileiro.Visible)
            {
                pessoa.DS_NTR = ddlDadosAcadCidade.SelectedItem.Text.Trim().ToUpper() + "-" + ddlDadosAcadUF.SelectedItem.Text.ToUpper();
            }
            else
            {
                pessoa.NM_ESTADONASCESTR = txtDadosAcadNAOBrEstado.Text.Trim().ToUpper();
                pessoa.NM_CIDADENASCESTR = txtDadosAcadNAOBrCidade.Text.Trim().ToUpper();

                //Limpa campos do Brasil
                pessoa.DS_NTR = string.Empty;
            }

            academicoPessoaDadosComplementares.GrauParentesco = ddlDadosAcadRespFinanceiro.SelectedValue;

            if (ddlDadosAcadRespFinanceiro.SelectedValue == "O")
                academicoPessoaDadosComplementares.NomeRespFinanceiro = txtDadosAcadNomeResponsavel.Text.Trim().ToUpper();
            else
                academicoPessoaDadosComplementares.NomeRespFinanceiro = string.Empty;

            academico.ID_FRMING = ddlDadosAcadFormaIngresso.SelectedValue;

            T_TLFPSS telefoneResidencial = pessoa.T_TLFPSS
                                                 .Select(o => o as T_TLFPSS)
                                                 .Where(o => (o.ID_TPITLF == Convert.ToInt32(ETelefone.Residencial).ToString()))
                                                 .OrderByDescending(d => d.D_ATUALIZACAO)
                                                 .FirstOrDefault();

            ////////////////////////////////////////////////////////////////// QUANDO FOR OBRIGATORIO
            //////////////////////////////////////////////////////////////////
            //if (telefoneResidencial != null)
            //{
            //    telefoneResidencial.NR_DDD = string.IsNullOrEmpty(txtTelefoneResidencialDDD.Text.Trim()) ? 0 : Convert.ToDecimal(txtTelefoneResidencialDDD.Text);
            //    telefoneResidencial.NR_TLF = string.IsNullOrEmpty(txtTelefoneResidencialNumero.Text.Trim()) ? 0 : Convert.ToDecimal(txtTelefoneResidencialNumero.Text);
            //    telefoneResidencial.D_ATUALIZACAO = dataDaAtualizacao;
            //}
            //else
            //{
            //    telefoneResidencial = new T_TLFPSS();
            //    telefoneResidencial.NR_DDI = 55;
            //    telefoneResidencial.NR_DDD = string.IsNullOrEmpty(txtTelefoneResidencialDDD.Text.Trim()) ? 0 : Convert.ToDecimal(txtTelefoneResidencialDDD.Text);
            //    telefoneResidencial.NR_TLF = string.IsNullOrEmpty(txtTelefoneResidencialNumero.Text.Trim()) ? 0 : Convert.ToDecimal(txtTelefoneResidencialNumero.Text);
            //    telefoneResidencial.ID_TPITLF = Convert.ToInt32(ETelefone.Residencial).ToString();
            //    telefoneResidencial.D_ATUALIZACAO = dataDaAtualizacao;
            //    pessoa.T_TLFPSS.Add(telefoneResidencial);
            //}

            if (telefoneResidencial != null)
            {
                if (string.IsNullOrEmpty(txtTelefoneResidencialDDD.Text.Trim()) && string.IsNullOrEmpty(txtTelefoneResidencialNumero.Text.Trim()))
                {
                    // No telefone residencial não tem a remoção porque la é obrigatório ter o número
                    pessoa.T_TLFPSS.Remove(telefoneResidencial);
                }
                else
                {
                    telefoneResidencial.NR_DDD = string.IsNullOrEmpty(txtTelefoneResidencialDDD.Text.Trim()) ? 0 : Convert.ToDecimal(txtTelefoneResidencialDDD.Text);
                    telefoneResidencial.NR_TLF = string.IsNullOrEmpty(txtTelefoneResidencialNumero.Text.Trim()) ? 0 : Convert.ToDecimal(txtTelefoneResidencialNumero.Text);
                    telefoneResidencial.D_ATUALIZACAO = dataDaAtualizacao;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(txtTelefoneResidencialDDD.Text.Trim()) && !string.IsNullOrEmpty(txtTelefoneResidencialNumero.Text.Trim()))
                {
                    telefoneResidencial = new T_TLFPSS();
                    telefoneResidencial.NR_DDI = 55;
                    telefoneResidencial.NR_DDD = Convert.ToDecimal(txtTelefoneResidencialDDD.Text);
                    telefoneResidencial.NR_TLF = Convert.ToDecimal(txtTelefoneResidencialNumero.Text);
                    telefoneResidencial.ID_TPITLF = Convert.ToInt32(ETelefone.Residencial).ToString();
                    telefoneResidencial.D_ATUALIZACAO = dataDaAtualizacao;
                    pessoa.T_TLFPSS.Add(telefoneResidencial);
                }
            }
            /////////////////////////////////////////////
            ////////////////////////////////////////////////////////////


            T_TLFPSS telefoneCelular = pessoa.T_TLFPSS.Select(o => o as T_TLFPSS)
                                             .Where(o => (o.ID_TPITLF == Convert.ToInt32(ETelefone.Celular).ToString()))
                                             .OrderByDescending(d => d.D_ATUALIZACAO)
                                             .FirstOrDefault();

            if (telefoneCelular != null)
            {
                if (string.IsNullOrEmpty(txtTelefoneCelularDDD.Text.Trim()) && string.IsNullOrEmpty(txtTelefoneCelularNumero.Text.Trim()))
                {
                    // No telefone residencial não tem a remoção porque la é obrigatório ter o número
                    pessoa.T_TLFPSS.Remove(telefoneCelular);
                }
                else
                {
                    telefoneCelular.NR_DDD = string.IsNullOrEmpty(txtTelefoneCelularDDD.Text.Trim()) ? 0 : Convert.ToDecimal(txtTelefoneCelularDDD.Text);
                    telefoneCelular.NR_TLF = string.IsNullOrEmpty(txtTelefoneCelularNumero.Text.Trim()) ? 0 : Convert.ToDecimal(txtTelefoneCelularNumero.Text);
                    telefoneCelular.D_ATUALIZACAO = dataDaAtualizacao;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(txtTelefoneCelularDDD.Text.Trim()) && !string.IsNullOrEmpty(txtTelefoneCelularNumero.Text.Trim()))
                {
                    telefoneCelular = new T_TLFPSS();
                    telefoneCelular.NR_DDI = 55;
                    telefoneCelular.NR_DDD = Convert.ToDecimal(txtTelefoneCelularDDD.Text);
                    telefoneCelular.NR_TLF = Convert.ToDecimal(txtTelefoneCelularNumero.Text);
                    telefoneCelular.ID_TPITLF = Convert.ToInt32(ETelefone.Celular).ToString();
                    telefoneCelular.D_ATUALIZACAO = dataDaAtualizacao;
                    pessoa.T_TLFPSS.Add(telefoneCelular);
                }
            }

            T_PSS_EML emailPrincipal = pessoa.T_PSS_EML
                                             .Select(o => o as T_PSS_EML)
                                             .Where(o => (o.CD_PSS == Convert.ToInt32(academico.CD_PSS)) &&
                                                         (o.TIPO == Convert.ToDecimal(EEmail.Principal))).FirstOrDefault();
            if (emailPrincipal != null)
            {
                emailPrincipal.EMAIL = txtEmailPrincipal.Text.Trim();
                emailPrincipal.D_ATUALIAZACAO = dataDaAtualizacao;
            }
            else
            {
                if (!string.IsNullOrEmpty(txtEmailPrincipal.Text.Trim()))
                {
                    emailPrincipal = new T_PSS_EML();
                    emailPrincipal.EMAIL = txtEmailPrincipal.Text.Trim();
                    emailPrincipal.TIPO = Convert.ToDecimal(EEmail.Principal);
                    emailPrincipal.D_ATUALIAZACAO = dataDaAtualizacao;
                    pessoa.T_PSS_EML.Add(emailPrincipal);
                }
            }

            T_PSS_EML emailAlternativo = pessoa.T_PSS_EML
                                               .Select(o => o as T_PSS_EML)
                                               .Where(o => (o.TIPO == Convert.ToDecimal(EEmail.Alternativo))).FirstOrDefault();
            if (emailAlternativo != null)
            {
                if (string.IsNullOrEmpty(txtEmailAlternativo.Text))
                {
                    // No email principal não tem a remoção porque la é obrigatório ter email
                    pessoa.T_PSS_EML.Remove(emailAlternativo);
                }
                else
                {
                    emailAlternativo.EMAIL = txtEmailAlternativo.Text.Trim();
                    emailAlternativo.D_ATUALIAZACAO = dataDaAtualizacao;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(txtEmailAlternativo.Text.Trim()))
                {
                    emailAlternativo = new T_PSS_EML();
                    emailAlternativo.EMAIL = txtEmailAlternativo.Text.Trim();
                    emailAlternativo.TIPO = Convert.ToDecimal(EEmail.Alternativo);
                    emailAlternativo.D_ATUALIAZACAO = dataDaAtualizacao;
                    pessoa.T_PSS_EML.Add(emailAlternativo);
                }
            }

            if (fsDocumentosPessoaisBrasileiroOuNaturalizado.Visible)
            {
                #region Documentos Pessoais - Documentos
                pessoa.NR_CPF = txtDocumentosCPF.Text.Trim();
                pessoa.NR_CI = txtDocumentosRG.Text.Trim();
                pessoa.NM_ORGEXPCI = txtDocumentosRGOrgEmissor.Text.Trim().ToUpper();
                pessoa.SG_ESTCI = ddlDocumentosRGOrgEmissorUF.SelectedItem.Value != "0" ? ddlDocumentosRGOrgEmissorUF.SelectedItem.Text.Trim() : string.Empty;
                pessoa.DT_EXPCI = dpDocumentosRGDataExpedicao.SelectedDate;
                pessoa.NR_TTLELT = string.IsNullOrEmpty(txtDocumentosTituloEleitorNumero.Text.Trim()) ? (decimal?)null : Convert.ToDecimal(txtDocumentosTituloEleitorNumero.Text);
                pessoa.NR_ZNOTTL = string.IsNullOrEmpty(txtDocumentosTituloEleitorZona.Text.Trim()) ? (decimal?)null : Convert.ToDecimal(txtDocumentosTituloEleitorZona.Text);
                pessoa.CD_ESTTTL = ddlDocumentosTituloEleitorUF.SelectedItem.Value != "0" ? ddlDocumentosTituloEleitorUF.SelectedItem.Text.Trim() : string.Empty;
                pessoa.CD_MNCTTL = ddlDocumentosTituloEleitorCidade.SelectedItem.Value == "0" ? (decimal?)null : Convert.ToDecimal(ddlDocumentosTituloEleitorCidade.SelectedItem.Value);
                pessoa.NM_MNCTTL = ddlDocumentosTituloEleitorCidade.SelectedItem.Text.Trim().ToUpper();
                #endregion

                #region Documentos Pessoais - Documento Militar
                academicoPessoaDadosComplementares.DocMilitar = txtDocumentosMilitarNumero.Text.Trim();
                academicoPessoaDadosComplementares.SerieMilitar = txtDocumentosMilitarSerie.Text.Trim();
                academicoPessoaDadosComplementares.CompMilitar = txtDocumentosMilitarComplemento.Text.Trim().ToUpper();
                academicoPessoaDadosComplementares.SitMilitar = txtDocumentosMilitarSituacao.Text.Trim().ToUpper();
                #endregion

                #region Documentos Pessoais - Outros Documentos
                academicoPessoaDadosComplementares.CPF_Mae = txtDocumentosCPFMae.Text.Trim();
                academicoPessoaDadosComplementares.CPF_Pai = txtDocumentosCPFPai.Text.Trim();
                academicoPessoaDadosComplementares.CPF_Resp = txtDocumentosCPFResp.Text.Trim();
                #endregion
            }
            else
            {
                #region Documentos Pessoais - Documentos (Estrangeiro)
                pessoa.NR_RNEESTR = txtDocumentosRNE.Text.Trim();
                pessoa.NR_ORGRNEESTR = txtDocumentosRNEOrgEmissor.Text.Trim();
                pessoa.NR_CPF = txtDocumentosCPFEstrangeiro.Text.Trim();
                #endregion

                #region Documentos Pessoais - Dados do Passaporte
                pessoa.NR_PASSESTR = txtDocumentosPassaporteNumero.Text.Trim().ToUpper();
                pessoa.DT_EMSPASSESTR = dpDocumentosPassaporteDataEmissao.SelectedDate;
                pessoa.DT_VALPASSESTR = dpDocumentosPassaporteDataValidade.SelectedDate;
                pessoa.NM_PAISPASSESTR = txtDocumentosPassaportePais.Text.Trim().ToUpper();
                #endregion
            }

            #region Endereço - Residencial
            T_ENDPSS enderecoResidencial = pessoa.T_ENDPSS
                                                 .Select(o => o as T_ENDPSS)
                                                 .Where(o => //(o.CD_PSS == academico.CD_PSS) &&
                                                             (o.ID_TPIEND == Convert.ToDecimal(EEndereço.Residencial).ToString())).FirstOrDefault();
            if (enderecoResidencial != null)
            {
                enderecoResidencial.NR_CEP = 00000;
                enderecoResidencial.FL_ENDBRASIL = true;
                enderecoResidencial.DS_END = txtEnderecoResidencialLogradouro.Text.Trim().ToUpper();
                enderecoResidencial.NR_END = txtEnderecoResidencialNumero.Text.Trim();
                enderecoResidencial.DS_CMPEND = txtEnderecoResidencialComplemento.Text.Trim().ToUpper();
                enderecoResidencial.DS_BRR = txtEnderecoResidencialBairro.Text.Trim().ToUpper();
                enderecoResidencial.NR_CEP = string.IsNullOrEmpty(txtEnderecoResidencialCEP.Text.Trim()) ? (decimal?)null : Convert.ToDecimal(txtEnderecoResidencialCEP.Text);
                enderecoResidencial.SG_EST = ddlEnderecoResidencialUF.SelectedItem.Text.Trim().ToUpper();
                enderecoResidencial.DS_LCL = ddlEnderecoResidencialCidade.SelectedItem.Text.Trim().ToUpper();
                enderecoResidencial.ID_ENDPRC = "1"; // 1 -> Principal // 0 -> Não principal
                //enderecoResidencial.ID_TPIEND -> Não altera na edição.
                enderecoResidencial.CD_PSA = 31; // 31 -> Brasil // dbSII.SII.T_PSA
                enderecoResidencial.D_ATUALIZACAO = dataDaAtualizacao;
            }
            else
            {
                enderecoResidencial = new T_ENDPSS();
                enderecoResidencial.FL_ENDBRASIL = true;
                enderecoResidencial.DS_END = txtEnderecoResidencialLogradouro.Text.Trim().ToUpper();
                enderecoResidencial.NR_END = txtEnderecoResidencialNumero.Text.Trim();
                enderecoResidencial.DS_CMPEND = txtEnderecoResidencialComplemento.Text.Trim().ToUpper();
                enderecoResidencial.DS_BRR = txtEnderecoResidencialBairro.Text.Trim().ToUpper();
                enderecoResidencial.NR_CEP = string.IsNullOrEmpty(txtEnderecoResidencialCEP.Text.Trim()) ? (decimal?)null : Convert.ToDecimal(txtEnderecoResidencialCEP.Text);
                enderecoResidencial.SG_EST = ddlEnderecoResidencialUF.SelectedItem.Text.Trim().ToUpper();
                enderecoResidencial.DS_LCL = ddlEnderecoResidencialCidade.SelectedItem.Text.Trim().ToUpper();
                enderecoResidencial.ID_ENDPRC = "1"; // 1 -> Principal // 0 -> Não principal
                enderecoResidencial.ID_TPIEND = Convert.ToDecimal(EEndereço.Residencial).ToString(); // 1 -> Residencial // 2 -> Correspondencia
                enderecoResidencial.CD_PSA = 31; // 31 -> Brasil // dbSII.SII.T_PSA
                enderecoResidencial.D_ATUALIZACAO = dataDaAtualizacao;
                pessoa.T_ENDPSS.Add(enderecoResidencial);
            }
            #endregion

            #region Endereço - Correspondência
            T_ENDPSS enderecoCorrespondencia = pessoa.T_ENDPSS
                                                     .Select(o => o as T_ENDPSS)
                                                     .Where(o => //(o.CD_PSS == academico.CD_PSS) &&
                                                                 (o.ID_TPIEND == Convert.ToDecimal(EEndereço.Correspondencia).ToString())).FirstOrDefault();

            if (enderecoCorrespondencia != null) // Edição
            {
                enderecoCorrespondencia.FL_ENDBRASIL = rblEnderecoTipoCorrespondencia.SelectedItem.Value == Convert.ToInt32(ETipoEndereço.No_Brasil).ToString();

                if ((bool)enderecoCorrespondencia.FL_ENDBRASIL)
                {
                    // Faz a verificação só por esse campo, pq na validação ele faz o usuário preencher tudo
                    if (string.IsNullOrEmpty(txtEnderecoCorrespondenciaLogradouro.Text.Trim()))
                    {
                        // O endereço residencial não possui remoção, pq la é obrigatório ter
                        pessoa.T_ENDPSS.Remove(enderecoCorrespondencia);
                    }
                    else
                    {
                        enderecoCorrespondencia.DS_END = txtEnderecoCorrespondenciaLogradouro.Text.Trim().ToUpper();
                        enderecoCorrespondencia.NR_END = txtEnderecoCorrespondenciaNumero.Text.Trim();
                        enderecoCorrespondencia.DS_CMPEND = txtEnderecoCorrespondenciaComplemento.Text.Trim().ToUpper();
                        enderecoCorrespondencia.DS_BRR = txtEnderecoCorrespondenciaBairro.Text.Trim().ToUpper();
                        enderecoCorrespondencia.NR_CEP = string.IsNullOrEmpty(txtEnderecoCorrespondenciaCEP.Text.Trim()) ? (decimal?)null : Convert.ToDecimal(txtEnderecoCorrespondenciaCEP.Text);
                        enderecoCorrespondencia.SG_EST = ddlEnderecoCorrespondenciaUF.SelectedItem.Text.Trim().ToUpper();
                        enderecoCorrespondencia.DS_LCL = ddlEnderecoCorrespondenciaCidade.SelectedItem.Text.Trim().ToUpper();
                        enderecoCorrespondencia.ID_ENDPRC = "0"; // 1 -> Principal // 0 -> Não principal
                        enderecoCorrespondencia.D_ATUALIZACAO = dataDaAtualizacao;
                        enderecoCorrespondencia.CD_PSA = 31; // 31 -> Brasil // dbSII.SII.T_PSA
                        //enderecoCorrespondencia.ID_TPIEND // Não altera na edição.

                        // Limpa o lixo dos campos de fora do Brasil
                        enderecoCorrespondencia.DS_ESTESTR = string.Empty;
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(txtEnderecoCorrespondenciaFORAEndereco.Text.Trim()))
                    {
                        // O endereço residencial não possui remoção, pq la é obrigatório ter
                        pessoa.T_ENDPSS.Remove(enderecoCorrespondencia);
                    }
                    else
                    {
                        enderecoCorrespondencia.DS_END = txtEnderecoCorrespondenciaFORAEndereco.Text.Trim().ToUpper();
                        enderecoCorrespondencia.DS_ESTESTR = txtEnderecoCorrespondenciaFORAEstadoDistrito.Text.Trim().ToUpper();
                        enderecoCorrespondencia.DS_LCL = txtEnderecoCorrespondenciaFORACidade.Text.Trim().ToUpper();
                        enderecoCorrespondencia.NR_CEP = string.IsNullOrEmpty(txtEnderecoCorrespondenciaFORACodigoPostal.Text) ? (decimal?)null : Convert.ToDecimal(txtEnderecoCorrespondenciaFORACodigoPostal.Text);
                        enderecoCorrespondencia.CD_PSA = Convert.ToDecimal(ddlEnderecoCorrespondenciaFORAPais.SelectedItem.Value);
                        enderecoCorrespondencia.ID_ENDPRC = "0"; // 1 -> Principal // 0 -> Não principal
                        enderecoCorrespondencia.D_ATUALIZACAO = dataDaAtualizacao;
                        //enderecoCorrespondencia.ID_TPIEND // Não altera na edição.

                        // Limpa o lixo dos campos do Brasil
                        enderecoCorrespondencia.NR_END = string.Empty;
                        enderecoCorrespondencia.DS_CMPEND = string.Empty;
                        enderecoCorrespondencia.DS_BRR = string.Empty;
                        enderecoCorrespondencia.SG_EST = string.Empty;
                    }
                }
            }
            else // Cadastro
            {
                enderecoCorrespondencia = new T_ENDPSS();
                enderecoCorrespondencia.FL_ENDBRASIL = rblEnderecoTipoCorrespondencia.SelectedItem.Value == Convert.ToInt32(ETipoEndereço.No_Brasil).ToString();

                if (rblEnderecoTipoCorrespondencia.SelectedItem.Value == Convert.ToInt32(ETipoEndereço.No_Brasil).ToString())
                {
                    if (!string.IsNullOrEmpty(txtEnderecoCorrespondenciaLogradouro.Text.Trim()))
                    {
                        enderecoCorrespondencia.DS_END = txtEnderecoCorrespondenciaLogradouro.Text.Trim().ToUpper();
                        enderecoCorrespondencia.NR_END = txtEnderecoCorrespondenciaNumero.Text.Trim();
                        enderecoCorrespondencia.DS_CMPEND = txtEnderecoCorrespondenciaComplemento.Text.Trim().ToUpper();
                        enderecoCorrespondencia.DS_BRR = txtEnderecoCorrespondenciaBairro.Text.Trim().ToUpper();
                        enderecoCorrespondencia.NR_CEP = string.IsNullOrEmpty(txtEnderecoCorrespondenciaCEP.Text) ? (decimal?)null : Convert.ToDecimal(txtEnderecoCorrespondenciaCEP.Text);
                        enderecoCorrespondencia.SG_EST = ddlEnderecoCorrespondenciaUF.SelectedItem.Text.Trim().ToUpper();
                        enderecoCorrespondencia.DS_LCL = ddlEnderecoCorrespondenciaCidade.SelectedItem.Text.Trim().ToUpper();
                        enderecoCorrespondencia.ID_ENDPRC = "0"; // 1 -> Principal // 0 -> Não principal
                        enderecoCorrespondencia.D_ATUALIZACAO = dataDaAtualizacao;
                        enderecoCorrespondencia.CD_PSA = 31; // 31 -> Brasil // dbSII.SII.T_PSA
                        enderecoCorrespondencia.ID_TPIEND = Convert.ToDecimal(EEndereço.Correspondencia).ToString();
                        pessoa.T_ENDPSS.Add(enderecoCorrespondencia);
                    }
                }
                else if (rblEnderecoTipoCorrespondencia.SelectedItem.Value == Convert.ToInt32(ETipoEndereço.Fora_do_Brasil).ToString())
                {
                    if (!string.IsNullOrEmpty(txtEnderecoCorrespondenciaFORAEndereco.Text.Trim()))
                    {
                        enderecoCorrespondencia.DS_END = txtEnderecoCorrespondenciaFORAEndereco.Text.Trim().ToUpper();
                        enderecoCorrespondencia.DS_ESTESTR = txtEnderecoCorrespondenciaFORAEstadoDistrito.Text.Trim().ToUpper();
                        enderecoCorrespondencia.DS_LCL = txtEnderecoCorrespondenciaFORACidade.Text.Trim().ToUpper();
                        enderecoCorrespondencia.NR_CEP = string.IsNullOrEmpty(txtEnderecoCorrespondenciaFORACodigoPostal.Text) ? (decimal?)null : Convert.ToDecimal(txtEnderecoCorrespondenciaFORACodigoPostal.Text);
                        enderecoCorrespondencia.CD_PSA = Convert.ToDecimal(ddlEnderecoCorrespondenciaFORAPais.SelectedItem.Value);
                        enderecoCorrespondencia.ID_ENDPRC = "0"; // 1 -> Principal // 0 -> Não principal
                        enderecoCorrespondencia.D_ATUALIZACAO = dataDaAtualizacao;
                        enderecoCorrespondencia.ID_TPIEND = Convert.ToDecimal(EEndereço.Correspondencia).ToString();
                        pessoa.T_ENDPSS.Add(enderecoCorrespondencia);
                    }
                }
            }
            #endregion

            #region Informações Escolares e Acadêmicas
            academicoPessoaDadosComplementares.ID_Escola = txtInformacoesIDEscola.Text.Trim().ToUpper();
            academicoPessoaDadosComplementares.AnoConclusao = string.IsNullOrEmpty(txtInformacoesEscolaAnoConclusao.Text) ? (int?)null : Convert.ToInt32(txtInformacoesEscolaAnoConclusao.Text);
            academicoPessoaDadosComplementares.ID_EscolaVestib = txtInformacoesIDIes.Text.Trim().ToUpper();
            academicoPessoaDadosComplementares.AnoVestibular = txtInformacoesAnoVestibular.Text.Trim().ToUpper();
            academicoPessoaDadosComplementares.DiscVestibular = txtInformacoesDiscVest.Text.Trim().ToUpper();
            academicoPessoaDadosComplementares.MatVestibular = txtInformacoesDiscVest.Text.Trim().ToUpper();

            academicoPessoaDadosComplementares.ID_EscolaTransf = txtInformacoesIDTransferido.Text.Trim().ToUpper();
            #endregion

            #region Prepara os documentos pendentes - Removo TODOS os antigos e adiciona os novos da página. Foi feito dessa forma, pois cada documento é um registro na tabela. Sendo que uma pessoa pode ter mais de um RA. Dessa forma mantenho os registros dos outros RAs e atualizo do RA atual que está sendo editado
            // Busca os documentos do RA especifico
            List<int> documentosAntigos = new List<int>();
            foreach (PessoaDocPendentes item in pessoa.PessoaDocPendentes)
                if (item.NR_ACD == academico.NR_ACD)
                    documentosAntigos.Add(item.ID_PssDocPendente);

            // Apaga todos os documentos antigos desse RA
            foreach (int item in documentosAntigos)
            {
                PessoaDocPendentes remover = pessoa.PessoaDocPendentes
                                                   .Select(o => o as PessoaDocPendentes)
                                                   .Where(o => o.ID_PssDocPendente == item).FirstOrDefault();
                pessoa.PessoaDocPendentes.Remove(remover);
            }

            // Adicionar Novos da tabela da página.
            DataTable colecaoDocumentosPendentes = SessaoGrdDocumentosPendentes;
            foreach (DataRow item in colecaoDocumentosPendentes.AsEnumerable())
            {
                string obs = item.Campo("Obs");

                pessoa.PessoaDocPendentes.Add(new PessoaDocPendentes()
                {
                    ID_Documento = Convert.ToInt32(item.Field<string>("ID_Documento")),
                    Obs = string.IsNullOrEmpty(obs) ? string.Empty : obs.ToUpper(),
                    ID_curso = academicoPessoaDadosComplementares.ID_Curso,
                    NR_ACD = academico.NR_ACD
                });
            }
            #endregion

            #region Prepara as Deficiências - Removo TODOS os antigos e adiciona os novos da página. Foi feito dessa forma, pois cada deficiência é um registro na tabela. Sendo que uma pessoa pode ter mais de um RA. Dessa forma mantenho os registros dos outros RAs e atualizo do RA atual que está sendo editado.
            // Busca os documentos do RA especifico
            List<int> deficienciasAntigas = new List<int>();
            foreach (PessoaDadosComplementares_Deficiencia item in pessoa.PessoaDadosComplementares_Deficiencia)
                if (item.NR_ACD == academico.NR_ACD)
                    deficienciasAntigas.Add(item.Id_tipoDeficiencia);

            // Apaga todos os documentos antigos desse RA
            foreach (int item in deficienciasAntigas)
            {
                PessoaDadosComplementares_Deficiencia remover = pessoa.PessoaDadosComplementares_Deficiencia
                                                                      .Select(o => o as PessoaDadosComplementares_Deficiencia)
                                                                      .Where(o => o.Id_tipoDeficiencia == item).FirstOrDefault();
                pessoa.PessoaDadosComplementares_Deficiencia.Remove(remover);
            }

            // Adicionar Novos da tabela da página.
            DataTable colecaoDeficiencia = SessaoGrdDeficiencia;
            foreach (DataRow item in colecaoDeficiencia.AsEnumerable())
            {
                PessoaDadosComplementares_Deficiencia deficiencia = new PessoaDadosComplementares_Deficiencia();
                deficiencia.Id_tipoDeficiencia = Convert.ToInt32(item.Field<string>("ID_tipoDeficiencia"));
                deficiencia.NR_ACD = academico.NR_ACD;
                pessoa.PessoaDadosComplementares_Deficiencia.Add(deficiencia);
            }
            #endregion

            return pessoa;
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    protected string[] RALivre()
    {
        try
        {
            //ZIM.PST.rAlunosPST raluno = new ZIM.PST.rAlunosPST();
            //// Graduação: 110000 a 150000
            //raluno.listaLiberados("1");   //////    quando for fazer o WS =>      /// qnd for adicionar ra de pos/extensao trocar para  8
            //if (raluno.Items.Count == 0)
            //    raluno.listaLiberados("8");
            //string[] ra = new string[2];
            //ra[0] = raluno.Items[0].c_ident_al;
            //ra[1] = raluno.Items[0].vf_dig;
            //raluno.atualiza(ra[0], "O", ra[0]);
            //return ra;




            // 'lstralunos' -> Tabela do ZIM que indica os tipos a serem passados para a obtenção dos RAs.
            // 'pin1'(csReservaRA) deve receber o 'vf_csmd'(lstralunos) referente ao tipo de RA exigido.
            // 15G => Graduação e EAD.
            // Esse programa seta o campo 'indicador' da tabela 'rAlunos' para 'E' de esperando.
            // 'L' é liberado. 'O' é ocupado.
            // Após concluir o cadastro, esse RA deve ser setado para 'O'.
            ZIM.ZimDAL zd = new ZIM.ZimDAL();
            string stringZim = zd.StringPrgZIM("csReservaRA", "&pin1=" + "15G");
            stringZim = stringZim.Replace(";", "");
            string[] ra = stringZim.Split('|');
            return ra;
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    protected void LiberaRaReservadoNoZIM()
    {
        // No cadastro, é reservado um RA para ser registrado no sistema.
        // E caso ocorra erro no cadastro, o RA é liberado.
        //if (!string.IsNullOrEmpty(hfEhCadastro.Value) && hfEhCadastro.Value.Equals("true"))
        //{
        //    if (Convert.ToDecimal(hfRA.Value) != 0)
        //    {
        //        ZIM.PST.rAlunosPST raluno = new ZIM.PST.rAlunosPST();
        //        raluno.atualiza(hfRA.Value, "L", hfRA.Value);
        //        hfRA.Value = string.Empty;
        //    }
        //}
    }

    protected void enviaEmailErroParaDTI(Exception ex, string BancosEnvolvidos, string RA)
    {
        string msg = "<b>Erro ao Atualizar informações do CADASTRO ACADÊMICO no " + BancosEnvolvidos + ".</b>: RA " + RA;
        msg += "<br /><b>Operador</b>: WEB";

        msg += !string.IsNullOrEmpty(Configuracoes.RF) ? " Configuracoes.RF: " + Configuracoes.RF : string.Empty;
        //msg += !string.IsNullOrEmpty(Configuracoes.RA) ? " Configuracoes.RA: " + Configuracoes.RA : string.Empty;

        if (ex.InnerException != null)
            msg += "<br /><br /><b>Descrição do Erro</b>: " + ex.InnerException.Message;

        msg += "<br /><b>Message</b>: " + ex.Message;
        msg += "<br /><b>StackTrace</b>: " + ex.StackTrace;
        msg += "<br /><b>Source</b>: " + ex.Source;
        msg += "<br /><b>TargetSite</b>: " + ex.TargetSite;

        string ambienteTrabalho = ((UCDB_MSC.EAmbiente)Enum.Parse(typeof(UCDB_MSC.EAmbiente), System.Configuration.ConfigurationManager.AppSettings["AmbienteDeTrabalho"])).ToString();

        PortalFuncoes.EnviarEmail("portal-l@ucdb.br", "[ERRO PORTAL UCDB - CADASTRO ACADÊMICO] (" + ambienteTrabalho.Trim() + ")", msg, "portal-l2@ucdb.br", "portal");
    }

    protected void txtDocumentosCPF_TextChanged(object sender, EventArgs e)
    {
        T_PSS pessoa = servico.getPessoa(txtDocumentosCPF.Text.Trim());
        if (pessoa != null)
        {
            windowManager.Localization.OK = "Sim";
            windowManager.Localization.Cancel = "Não";
            string msg = "Este CPF já existe em nosso banco de dados.<br>" +
                         "Deseja utilizar essas informações para sobrescrever as digitadas na tela?<br>" +
                         "Lembrando que serão mantidas as informações da última atualização para essa pessoa.";
            windowManager.RadConfirm(msg, "confirmCarregarPessoaExistente", 600, 100, null, "Atenção!");
        }
    }

    protected void btnCarregarPessoaExistente_Click(object sender, EventArgs e)
    {
        string commandArgument = Request.Params.Get("__EVENTARGUMENT").ToString();

        if (!string.IsNullOrEmpty(commandArgument))
        {
            if (commandArgument.Equals("true"))
            {
                rblNaturalidade.Enabled = false;
                hfCPF.Value = txtDocumentosCPF.Text.Trim();
                hfRA.Value = "0";
                CarregaRegistroParaEdicao();
            }
        }
    }

    protected void txtTelefoneCelularNumero_TextChanged(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(txtTelefoneCelularNumero.Text))
        {
            if (Convert.ToDecimal(txtTelefoneCelularNumero.Text).ToString().Length == 8)
                txtTelefoneCelularNumero.Text = Convert.ToDecimal(txtTelefoneCelularNumero.Text).ToString().PadLeft(9, '0');
        }
    }

    protected void gnEscolaridadeBrasil_Click(object sender, EventArgs e)
    {
        abrirModalCadastroEscola();
        CampoBrasil.Visible = true;

        CampoEstrangeiroPais.Visible = false;
        CampoEstrangeiroDistrito.Visible = false;
        CampoEstrangeiroCidade.Visible = false;

    }

    protected void gnEscolaridadeEstrangeiro_Click(object sender, EventArgs e)
    {
        abrirModalCadastroEscola();
        CampoBrasil.Visible = false;

        CampoEstrangeiroPais.Visible = true;
        CampoEstrangeiroDistrito.Visible = true;
        CampoEstrangeiroCidade.Visible = true;

    }

    protected void chkRegistrado_Consulado_CheckedChanged(Object sender, EventArgs args)
    {
        txtDadosAcadNAOBrCidade.Text = string.Empty;
        txtDadosAcadNAOBrEstado.Text = string.Empty;

        trEstadoNAOBrasileiro.Visible = false;
        trCidadeNAOBrasileiro.Visible = false;
        trCidadeEstadoBrasileiro.Visible = true;

        if ((sender as CheckBox).Checked)
        {
            trEstadoNAOBrasileiro.Visible = true;
            trCidadeNAOBrasileiro.Visible = true;
            trCidadeEstadoBrasileiro.Visible = false;
            ddlDadosAcadUF.SelectedValue = null;
            ddlDadosAcadCidade.SelectedValue = null;
        }
    }

    protected void ckbNomeSocial_CheckedChanged(object sender, EventArgs e)
    {
        nomeSocial.Visible = ckbNomeSocial.Checked;
    }
}