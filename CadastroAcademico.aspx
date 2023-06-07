<%@ Page Language="C#" AutoEventWireup="true"
    EnableEventValidation="false" Inherits="GerencialPeriodoLetivo_CadastroAcademico"
    ValidateRequest="false" MaintainScrollPositionOnPostback="true" Theme="PortalTelerik"
    StylesheetTheme="PortalNovo" Culture="pt-BR" UICulture="pt-BR" CodeBehind="CadastroAcademico.aspx.cs" %>
<%@ Import Namespace="RestSharp.Extensions" %>

<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="telerik" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <style type="text/css">
        .controlPadrao {
            float: left;
            margin-left: 5px;
        }

        input {
            text-transform: uppercase;
        }

        .controlPadrao:focus {
            background: #f1faff;
            border: 1px solid #666565;
            outline: none;
        }

        .red {
            color: Red;
            margin-left: 3px;
        }

        #mostrarModal {
            cursor: pointer;
        }

        .modal_padrao {
            left: 50%;
            top: 50%; /*margin: -225px auto auto -300px;*/
            background-color: #fafafa;
            position: fixed;
            z-index: 1002;
            display: none;
            border: 4px solid #435e75;
        }

            .modal_padrao #fecharModal {
                cursor: pointer;
                float: right;
                padding: 10px;
                font-size: 15px !important;
                font-weight: bold;
                text-align: right;
            }

            .modal_padrao #fecharModalAviso {
                cursor: pointer;
                float: right;
                padding: 10px;
                font-size: 15px !important;
                font-weight: bold;
                text-align: right;
            }

            .modal_padrao .conteudo {
                margin: 20px;
            }

        .topo_popup {
            margin: 5px 0 -10px 5px;
        }

        .counter {
            float: left;
            font-weight: bold;
        }

        .tablePaseescolas td {
            padding: 3px;
        }

        .aviso {
            font: italic 14px "Segoe ui", "Trebuchet MS", Tahoma, Arial, Sans-serif;
            background: #fbefef;
            color: #650f0f;
            width: 660px;
            margin: 0px auto;
            padding: 5px 0;
            vertical-align: middle;
            text-align: center;
            margin-top: 5px;
        }
    </style>
    <telerik:RadScriptBlock ID="RadScriptBlock1" runat="server">
        <script type="text/javascript" src="http://ajax.googleapis.com/ajax/libs/jquery/1.4.2/jquery.min.js"></script>
        <script type="text/javascript" src="http://ajax.googleapis.com/ajax/libs/jqueryui/1.8.1/jquery-ui.min.js"></script>
        <script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
        <script type="text/javascript">
            function consultarCEP(cep) {
                cep = cep.replace(/\D/g, '');

                var logradouroInput = document.getElementById('<%= txtEnderecoResidencialLogradouro.ClientID %>');
                var bairroInput = document.getElementById('<%= txtEnderecoResidencialBairro.ClientID %>');
                var ufInput = document.getElementById('<%= ddlEnderecoResidencialUF.ClientID %>');
                var cidadeInput = document.getElementById('<%= ddlEnderecoResidencialCidade.ClientID %>');
                console.log(cidadeInput);
                if (cep.length >= 8) {

                    $.getJSON('https://viacep.com.br/ws/' + cep + '/json/', function (data) {
                        if (!data.erro) {

                            if (data.logradouro != null) {
                                logradouroInput.value = data.logradouro;
                                logradouroInput.readOnly = true;
                            }

                            if (data.localidade != null) {
                                cidadeInput.value = data.localidade;
                                cidadeInput.readOnly = true;
                            }

                            if (data.uf != null) {
                                ufInput.value = data.uf;
                                //ufInput.set_readOnly(true);
                            }

                            if (data.bairro != null) {
                                bairroInput.value = data.bairro;
                                bairroInput.readOnly = true;
                            }
                        } else {
                            //ufInput.set_readOnly(false);
                            cidadeInput.readOnly = false;
                            bairroInput.readOnly = false;
                            logradouroInput.readOnly = false;
                        }
                    });
                } else {
                    //ufInput.set_readOnly(false);
                    cidadeInput.readOnly = false;
                    bairroInput.readOnly = false;
                    logradouroInput.readOnly = false;
                }
            }
        </script>
        <script type="text/javascript">
            function setupAutocomplete(sender) {
                var cepInput = document.getElementById('<%= txtEnderecoResidencialCEP.ClientID %>');
                cepInput.addEventListener('blur', function () {
                    consultarCEP($(this).val());
                });
            }
        </script>
        <script type="text/javascript">

            function redirecionaCadastro() {
                window.location.href = 'CadastroAcademico.aspx?d=' + document.getElementById("<%= hfRecarregaPaginaParaEdicao.ClientID%>").value;
            }

            function redirecionaConsultaAcademico() {
                window.location.href = 'ConsultaAcademico.aspx';
            }

            function fechaJanela() {
                alert('Dados do acadêmico salvos com sucesso!\n\nAguarde o carregamento da Tela de Matrícula!');
                parent.location.href = 'Matricula.aspx';
            }

            function Close() {
                var oWnd = GetRadWindow(); // Get reference to current radwindow
                if (oWnd != null)
                    oWnd.close();
                else
                    window.close();
            }

            function GetRadWindow() {
                var oWindow = null;
                if (window != null) {
                    if (window.radWindow)
                        oWindow = window.radWindow;
                    else if (window.frameElement != null) {
                        if (window.frameElement.radWindow)
                            oWindow = window.frameElement.radWindow;
                    }
                }
                return oWindow;
            }

            function confirmaSalvar(args, event) {
                var msg = "";
                var URL = document.location.href;

                if (URL.indexOf("d=") > -1)
                    msg = "Atenção!\n\nAs novas informações inseridas irão substituir aquelas que já estavam salvas antes.\n\nConfirma a alteração?";
                else
                    msg = "Atenção!\n\nConfirma salvar as informações do acadêmico?";

                if (confirm(msg))
                    args.set_autoPostBack(true);
                else
                    args.set_autoPostBack(false);
            }

            $(document).ready(function () {
                $(".mostrarModal").click(function () {
                    $("#mask").css('opacity', 0.3).fadeIn();
                    $("#modal").fadeIn();
                });

                $("#fecharModalAviso").click(function () {
                    $("#mask").fadeOut();
                    $("#modalAviso").fadeOut();
                });

                // controls character input/counter
                $('#txtInfCompDisc').keyup(function () {
                    var charLength = $(this).val().length;
                    // Displays count
                    if ($(this).val().length <= 30)
                        $('#counter').html('1º Linha');

                    if ($(this).val().length > 30 && $(this).val().length <= 60)
                        $('#counter').html('2º Linha');

                    if ($(this).val().length > 60 && $(this).val().length <= 90)
                        $('#counter').html('3º Linha');

                    if ($(this).val().length > 90 && $(this).val().length <= 120)
                        $('#counter').html('4º Linha');

                    if ($(this).val().length > 120 && $(this).val().length <= 150)
                        $('#counter').html('5º Linha');

                    if ($(this).val().length > 120)
                        $('#counter').html('6º Linha limite');
                });
            });

            function fecharModalAviso() {
                document.getElementById('modalAviso').style.display = 'none';
                document.getElementById('mask').style.display = 'none';
            }

            function fecharModal3() {
                document.getElementById('modalCadastroEscolas').style.display = 'none';
                document.getElementById('mask').style.display = 'none';
            }

            ////////////////////////////////////////////////////////////////////// UTILIZADO PELOS MODAIS TOOLTIP - INICIO
            function CloseToolTip() {
                var tooltip = Telerik.Web.UI.RadToolTip.getCurrent();

                if (tooltip)
                    tooltip.hide();
            }
            function ClientShow(sender, args) {
            }
            function ManualClose(sender, args) {
            }
            function FromCode(sender, args) {
            }
            //////////////////////////////////////////////////////////////////////  UTILIZADO PELOS MODAIS TOOLTIP - FIM

            // Caso exista alguma pessoa com o CPF indicado, é carregado após o usuário clicar SIM no alert de confirmação.
            // Esse alert é disparado no evento do text box de cpf
            function confirmCarregarPessoaExistente(arg) {
                __doPostBack("<%= btnCarregarPessoaExistente.UniqueID %>", arg);
            }

            function confirmVoltaPaginaConsultaAcademico(arg) {
                __doPostBack("<%= btnVoltaPaginaConsultaAcademico.UniqueID %>", arg);
            }

            // seleciona o texto da drop quando ela é focada. 
            // para que o usuário possa alterar o texto logo quando o foco cai no ddl
            function selecionaTextoDDL(sender, eventArgs) {
                sender.get_inputDomElement().select();
            }


            /////////////////////////////////////////////////////////////////////////////////////////////////
            // Função para os telerik:RadDatePicker 
            // Há um problema nesse componente do Telerik por causa do horário de verão.
            // Todas as datas serão setadas para 1 AM da manha para não ter problema.
            // Um exemplo de data que dá problema é o 19/10/1980
            // quando vc valoriza com essas datas com problema ele seta uma data anterior. 
            // no exemplo anterior seta para 18/10/1980 no visual, mas no value ele fica com 19 mesmo.
            /////////////////////////////////////////////////////////////////////////////////////////////////
            function valueChanging(sender, args) {
                if (args == null)
                    return;
                //alert("string:" + args.get_newValue().toString() + " <br> length:" + args.get_newValue().toString().length);
                // 19/10/1980 == 10 // 19101980 == 8
                if (!(args.get_newValue().toString().length == 10 || args.get_newValue().toString().length == 8))
                    return;

                //args.set_newValue("19/10/1980 01:00 AM");
                if (args.get_newValue().toString().length == 10) {
                    args.set_newValue(args.get_newValue().toString().substr(0, 10) + " 01:00 AM");
                }
                else if (args.get_newValue().toString().length == 8) {
                    args.set_newValue(args.get_newValue().toString().substr(0, 2) + "/" +
                        args.get_newValue().toString().substr(2, 2) + "/" +
                        args.get_newValue().toString().substr(4, 4) +
                        " 01:00 AM");
                }
                //alert(args.get_newValue());
            }
            /////////////////////////////////////////////////////////////////////////////////////////////////
            // Fim da função utilizada pelo telerik:RadDatePicker ///////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////////////////////
        </script>
    </telerik:RadScriptBlock>
    <title>Cadastro Acadêmico - Secretaria</title>
</head>
<body>
    <form id="form1" runat="server">
        <telerik:RadAjaxPanel ID="RadAjaxPanel1" runat="server" Style="margin: 0 10px 10px 10px;"
            Width="99%">
            <div id="content" runat="server">
                <telerik:RadScriptManager ID="scriptManager" runat="server" EnableScriptGlobalization="true">
                    <Scripts>
                        <asp:ScriptReference Assembly="Telerik.Web.UI" Name="Telerik.Web.UI.Common.Core.js"></asp:ScriptReference>
                        <asp:ScriptReference Assembly="Telerik.Web.UI" Name="Telerik.Web.UI.Common.jQuery.js"></asp:ScriptReference>
                        <asp:ScriptReference Assembly="Telerik.Web.UI" Name="Telerik.Web.UI.Common.jQueryInclude.js"></asp:ScriptReference>
                    </Scripts>
                </telerik:RadScriptManager>
                <h1 class="titulo">
                    <img src="../App_Themes/PortalTelerik/img/h1icon.png" alt="" title="Consulta e Cadastro de Acadêmicos" />
                    Cadastro de Acadêmicos</h1>
                <p class="instrucoes">
                    Utilize os formulários abaixo para cadastrar as informações do acadêmico.
               
                    <br />
                    Os campos marcados com asterisco (<span class="red">*</span>) são de preenchimento
                obrigatório.
               
                </p>
                <asp:HiddenField ID="hfOrigem" runat="server" />
                <asp:HiddenField ID="hfRA" runat="server" />
                <asp:HiddenField ID="hfEhCadastro" runat="server" />
                <asp:HiddenField ID="hfCPF" runat="server" />
                <asp:HiddenField ID="hfID_Curso" runat="server" />
                <asp:HiddenField ID="hfRecarregaPaginaParaEdicao" runat="server" />
                <telerik:RadAjaxLoadingPanel ID="loadPanel" runat="server" Skin="Windows7" IsSticky="True"
                    Style="position: fixed; top: 0; right: 0; bottom: 0; left: 0; height: 100%; width: 100%; margin: 0; padding: 0; z-index: 100"
                    Transparency="0">
                </telerik:RadAjaxLoadingPanel>
                <fieldset class="fieldsetStyle" style="width: 700px;">
                    <legend class="legendFieldset">Curso</legend>
                    <br />
                    <table>
                        <tr>
                            <td>
                                <asp:Label ID="Label1" runat="server" Text="Curso" SkinID="labelAzul"></asp:Label>
                            </td>
                            <td>
                                <telerik:RadComboBox ID="ddlCursos" runat="server" DataTextField="n_completo_cr"
                                    Filter="Contains" EnableEmbeddedSkins="false" Skin="PortalTelerik" DataValueField="c_ident_cr"
                                    Width="530px" AppendDataBoundItems="True" MarkFirstMatch="true" OnItemDataBound="ddlCursos_ItemDataBound">
                                </telerik:RadComboBox>
                                <span class="red">*</span>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:Label ID="Label2" runat="server" Text="Pólo" SkinID="labelAzul">
                                </asp:Label>
                            </td>
                            <td>
                                <telerik:RadComboBox ID="ddlPolo" runat="server" MarkFirstMatch="True" Width="530px"
                                    EnableEmbeddedSkins="false" Skin="PortalTelerik" AppendDataBoundItems="true"
                                    Filter="Contains">
                                </telerik:RadComboBox>
                                <span class="red">*</span>
                            </td>
                        </tr>
                    </table>
                    <br />
                </fieldset>
                <fieldset class="fieldsetStyle" style="width: 700px;">
                    <legend class="legendFieldset">Naturalidade</legend>
                    <br />
                    <center>
                        <asp:RadioButtonList ID="rblNaturalidade" runat="server" AutoPostBack="true" RepeatDirection="Horizontal"
                            OnSelectedIndexChanged="rblNaturalidade_SelectedIndexChanged">
                        </asp:RadioButtonList>
                    </center>
                    <br />
                </fieldset>
                <fieldset class="fieldsetStyle" style="width: 700px;">
                    <legend class="legendFieldset">Dados do Acadêmico</legend>
                    <br />
                    <table>
                        <tr>
                            <td>
                                <asp:Label ID="Label3" runat="server" Text="RA" SkinID="labelAzul" Style="margin-left: 24px"></asp:Label>
                            </td>
                            <td>
                                <asp:Label ID="lblDadosAcadRA" runat="server" Font-Bold="true" Font-Size="Medium"
                                    Style="margin-left: 5px">
                                </asp:Label>
                            </td>
                        </tr>

                        <tr>
                            <td>
                                <asp:Label ID="labelCkbNomeSocial" runat="server" AssociatedControlID="ckbNomeSocial" Text="Exibir Nome Social"
                                    SkinID="labelAzul" Style="margin-left: 24px">
                                </asp:Label>
                            </td>
                            <td>
                                <asp:CheckBox ID="ckbNomeSocial" runat="server" OnCheckedChanged="ckbNomeSocial_CheckedChanged" AutoPostBack="true"></asp:CheckBox>
                            </td>
                        </tr>

                        <tr id="nomeSocial" runat="server" visible="false">
                            <td>
                                <asp:Label ID="labelNomeSocial" runat="server" Text="Nome" SkinID="labelAzul" Style="margin-left: 24px">
                                </asp:Label>
                            </td>
                            <td>
                                <asp:TextBox ID="txtNomeSocial" runat="server" SkinID="txtMedio" MaxLength="100"
                                    Width="320px">
                                </asp:TextBox>
                                <span class="red">*</span>
                            </td>
                        </tr>

                        <tr>
                            <td>
                                <% if (ckbNomeSocial.Checked)
                                    {%>
                                <asp:Label ID="Label70" runat="server" Text="Nome Social" SkinID="labelAzul" Style="margin-left: 24px">
                                </asp:Label>
                                <% }
                                    else
                                    {
                                %>
                                <asp:Label ID="Label4" runat="server" Text="Nome" SkinID="labelAzul" Style="margin-left: 24px">
                                </asp:Label>
                            </td>
                            <% } %>
                            <td>
                                <asp:TextBox ID="txtDadosAcadNome" runat="server" SkinID="txtMedio" MaxLength="100"
                                    Width="320px">
                                </asp:TextBox>
                                <span class="red">*</span>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:Label ID="Label5" runat="server" Text="Nome da Mãe" SkinID="labelAzul" Style="margin-left: 24px"></asp:Label>
                            </td>
                            <td>
                                <asp:TextBox ID="txtDadosAcadNomeMae" runat="server" SkinID="txtMedio" MaxLength="100"
                                    Width="320px"></asp:TextBox>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:Label ID="Label6" runat="server" Text="Nome do Pai" SkinID="labelAzul" Style="margin-left: 24px"></asp:Label>
                            </td>
                            <td>
                                <asp:TextBox ID="txtDadosAcadNomePai" runat="server" SkinID="txtMedio" MaxLength="100"
                                    Width="320px"></asp:TextBox>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:Label ID="Label7" runat="server" Text="Sexo" SkinID="labelAzul" Style="margin-left: 24px">
                                </asp:Label>
                            </td>
                            <td>
                                <asp:RadioButtonList ID="rblDadosAcadSexo" runat="server" RepeatLayout="Flow" RepeatDirection="Horizontal"
                                    Style="margin-left: 4px">
                                    <asp:ListItem Value="M" Text="Masculino" />
                                    <asp:ListItem Value="F" Text="Feminino" />
                                </asp:RadioButtonList>
                                <span class="red">*</span>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:Label ID="Label8" runat="server" Text="Estado Civil" SkinID="labelAzul" Style="margin-left: 24px"></asp:Label>
                            </td>
                            <td>
                                <telerik:RadComboBox ID="ddlDadosAcadEstCivil" runat="server" Width="326px" AppendDataBoundItems="true"
                                    EnableEmbeddedSkins="false" Skin="PortalTelerik" Style="margin-left: 5px">
                                </telerik:RadComboBox>
                                <span class="red">*</span>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:Label ID="Label9" runat="server" Text="Data de Nascimento" SkinID="labelAzul"
                                    Style="margin-left: 24px"></asp:Label>
                            </td>
                            <td>
                                <telerik:RadDatePicker ID="dpDadosAcadNascimento" runat="server" Width="97px" MinDate="1910-01-01"
                                    Culture="pt-BR" EnableEmbeddedSkins="false" Skin="PortalTelerik" Style="margin-left: 5px"
                                    ShowPopupOnFocus="true" DateInput-ClientEvents-OnValueChanging="valueChanging"
                                    DateInput-ClientEvents-OnValueChanged="valueChanging" DateInput-ClientEvents-OnBlur="valueChanging"
                                    DateInput-Calendar-ClientEvents-OnDateSelected="valueChanging">
                                    <Calendar runat="server" CultureInfo="pt-BR" ViewSelectorText="x" Skin="PortalTelerik"
                                        EnableEmbeddedSkins="False">
                                    </Calendar>
                                    <DateInput runat="server" DisplayDateFormat="dd/MM/yyyy" DateFormat="dd/MM/yyyy"
                                        Culture="pt-BR" EnableSingleInputRendering="false" LabelWidth="64px" EnableEmbeddedSkins="False">
                                    </DateInput>
                                    <DatePopupButton runat="server" ImageUrl="" HoverImageUrl=""></DatePopupButton>
                                </telerik:RadDatePicker>
                                <span class="red">*</span>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:Label ID="Label10" runat="server" Text="Nacionalidade" SkinID="labelAzul" Style="margin-left: 24px"></asp:Label>
                            </td>
                            <td>
                                <telerik:RadComboBox ID="ddlDadosAcadNacionalidade" runat="server" Width="326px"
                                    MarkFirstMatch="true" EnableEmbeddedSkins="false" Skin="PortalTelerik" Visible="false"
                                    Style="margin-left: 5px" Height="300px" OnItemDataBound="ddlDadosAcadNacionalidade_ItemDataBound">
                                </telerik:RadComboBox>
                                <asp:Label ID="lblDadosAcadNacionalidade" Text="BRASILEIRA" runat="server" Style="margin-left: 5px"></asp:Label>
                                <span class="red">*</span>
                                <asp:CheckBox ID="chkRegistrado_Consulado" runat="server" OnCheckedChanged="chkRegistrado_Consulado_CheckedChanged"
                                    Text="Registrado em Consulado" AutoPostBack="true" Style="margin-left: 5px"></asp:CheckBox>
                            </td>
                            <td></td>
                        </tr>
                        <tr id="trCidadeEstadoBrasileiro" runat="server">
                            <td>
                                <asp:Label ID="Label11" runat="server" Text="Estado / Cidade" SkinID="labelAzul"
                                    Style="margin-left: 24px"></asp:Label>
                            </td>
                            <td>
                                <telerik:RadComboBox ID="ddlDadosAcadUF" runat="server" MarkFirstMatch="True" EnableEmbeddedSkins="false"
                                    Skin="PortalTelerik" AppendDataBoundItems="true" AutoPostBack="True" Width="50px"
                                    Height="200px" OnSelectedIndexChanged="ddlDadosAcadUF_SelectedIndexChanged" Style="margin-left: 5px">
                                </telerik:RadComboBox>
                                <span class="red">*</span> &nbsp;
                           
                                <telerik:RadComboBox ID="ddlDadosAcadCidade" runat="server" Enabled="false" AppendDataBoundItems="true"
                                    MarkFirstMatch="True" Width="257px" Height="200px" EnableEmbeddedSkins="false"
                                    OnClientFocus="selecionaTextoDDL" Skin="PortalTelerik">
                                </telerik:RadComboBox>
                                <span class="red">*</span>
                            </td>
                        </tr>
                        <tr id="trEstadoNAOBrasileiro" runat="server">
                            <td>
                                <asp:Label ID="Label12" runat="server" Text="Estado / Distrito" SkinID="labelAzul"
                                    Style="margin-left: 24px"></asp:Label>
                            </td>
                            <td>
                                <asp:TextBox ID="txtDadosAcadNAOBrEstado" runat="server" SkinID="txtMedio" MaxLength="100"
                                    Style="margin-left: 5px" Width="320px"></asp:TextBox>
                                <span class="red">*</span>
                            </td>
                        </tr>
                        <tr id="trCidadeNAOBrasileiro" runat="server">
                            <td>
                                <asp:Label ID="Label13" runat="server" Text="Cidade" SkinID="labelAzul" Style="margin-left: 24px"></asp:Label>
                            </td>
                            <td>
                                <asp:TextBox ID="txtDadosAcadNAOBrCidade" runat="server" SkinID="txtMedio" MaxLength="100"
                                    Style="margin-left: 5px" Width="320px"></asp:TextBox>
                                <span class="red">*</span>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:Label ID="Label14" runat="server" Text="Resp. Financeiro" SkinID="labelAzul"
                                    Style="margin-left: 24px"></asp:Label>
                            </td>
                            <td>
                                <telerik:RadComboBox ID="ddlDadosAcadRespFinanceiro" runat="server" MarkFirstMatch="True"
                                    AppendDataBoundItems="true" Width="326px" EnableEmbeddedSkins="false" Skin="PortalTelerik"
                                    AutoPostBack="true" OnSelectedIndexChanged="ddlDadosAcadRespFinanceiro_SelectedIndexChanged"
                                    Style="margin-left: 5px">
                                    <Items>
                                        <telerik:RadComboBoxItem Text="O MESMO" Value="A" />
                                        <telerik:RadComboBoxItem Text="MÃE" Value="M" />
                                        <telerik:RadComboBoxItem Text="PAI" Value="P" />
                                        <telerik:RadComboBoxItem Text="OUTRO" Value="O" />
                                    </Items>
                                </telerik:RadComboBox>
                            </td>
                        </tr>
                        <tr runat="server" id="trTxtDadosAcadNomeResponsavel" visible="false">
                            <td>
                                <asp:Label ID="Label15" runat="server" Text="Nome do Resp." SkinID="labelAzul" Style="margin-left: 24px"></asp:Label>
                            </td>
                            <td>
                                <asp:TextBox ID="txtDadosAcadNomeResponsavel" runat="server" MaxLength="100" Style="margin-left: 5px"
                                    Width="320px"></asp:TextBox>
                                <span class="red">*</span>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:Label ID="Label16" runat="server" Text="Forma Ingresso" SkinID="labelAzul" Style="margin-left: 24px"></asp:Label>
                            </td>
                            <td>
                                <telerik:RadComboBox ID="ddlDadosAcadFormaIngresso" runat="server" Width="326px"
                                    AppendDataBoundItems="true" EnableEmbeddedSkins="false" Skin="PortalTelerik"
                                    Style="margin-left: 5px">
                                </telerik:RadComboBox>
                                <span class="red">*</span>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:Label ID="Label69" runat="server" Text="Cor / Raça" SkinID="labelAzul" Style="margin-left: 24px"></asp:Label>
                            </td>
                            <td>
                                <telerik:RadComboBox ID="ddlDadosAcadCorRaca" runat="server" Width="180px"
                                    AppendDataBoundItems="true" EnableEmbeddedSkins="false" Skin="PortalTelerik"
                                    Style="margin-left: 5px">
                                </telerik:RadComboBox>
                            </td>
                        </tr>
                    </table>
                    <fieldset class="fieldsetStyle">
                        <legend class="legendFieldset">Telefone</legend>
                        <br />
                        <table>
                            <tr>
                                <td>
                                    <asp:Label ID="Label17" runat="server" Text="Residencial" SkinID="labelAzul"></asp:Label>
                                </td>
                                <td>
                                    <telerik:RadNumericTextBox ID="txtTelefoneResidencialDDD" runat="server" Width="30px"
                                        MaxLength="2" EnableEmbeddedSkins="false" Skin="PortalTelerik" Style="margin-left: 8px; margin-right: 3px;">
                                        <NumberFormat DecimalDigits="0" ZeroPattern="n" />
                                        <IncrementSettings InterceptArrowKeys="False" InterceptMouseWheel="False" />
                                    </telerik:RadNumericTextBox>
                                    -
                               
                                    <telerik:RadMaskedTextBox ID="txtTelefoneResidencialNumero" runat="server" Mask="####-#####"
                                        EnableEmbeddedSkins="false" Skin="PortalTelerik" Style="margin-left: 3px;">
                                    </telerik:RadMaskedTextBox>
                                    <%--<span class="red">*</span>--%>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label ID="Label18" runat="server" Text="Celular" SkinID="labelAzul"></asp:Label>
                                </td>
                                <td>
                                    <telerik:RadNumericTextBox ID="txtTelefoneCelularDDD" runat="server" Width="30px"
                                        MaxLength="2" EnableEmbeddedSkins="false" Skin="PortalTelerik" Style="margin-left: 8px; margin-right: 3px;">
                                        <NumberFormat DecimalDigits="0" ZeroPattern="n" />
                                        <IncrementSettings InterceptArrowKeys="False" InterceptMouseWheel="False" />
                                    </telerik:RadNumericTextBox>
                                    -
                               
                                    <telerik:RadMaskedTextBox ID="txtTelefoneCelularNumero" runat="server" Mask="#####-####"
                                        EnableEmbeddedSkins="false" Skin="PortalTelerik" Style="margin-left: 3px;" OnTextChanged="txtTelefoneCelularNumero_TextChanged"
                                        AutoPostBack="true">
                                    </telerik:RadMaskedTextBox>
                                </td>
                            </tr>
                        </table>
                        <br />
                    </fieldset>
                    <fieldset class="fieldsetStyle">
                        <legend class="legendFieldset">E-mail</legend>
                        <br />
                        <table>
                            <tr>
                                <td>
                                    <asp:Label ID="Label19" runat="server" Text="E-mail Principal" SkinID="labelAzul"></asp:Label>
                                </td>
                                <td>
                                    <asp:TextBox ID="txtEmailPrincipal" runat="server" SkinID="txtMedio" MaxLength="50"
                                        Style="margin-left: 8px;"></asp:TextBox>
                                    <span class="red">*</span>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label ID="Label20" runat="server" Text="E-mail Alternativo" SkinID="labelAzul"></asp:Label>
                                </td>
                                <td>
                                    <asp:TextBox ID="txtEmailAlternativo" runat="server" SkinID="txtMedio" MaxLength="50"
                                        Style="margin-left: 8px; margin-right: 5px"></asp:TextBox>
                                </td>
                            </tr>
                        </table>
                        <br />
                    </fieldset>
                    <br />
                </fieldset>
                <fieldset class="fieldsetStyle" style="width: 700px;" runat="server" id="fsDocumentosPessoaisBrasileiroOuNaturalizado">
                    <legend class="legendFieldset">Documentos&nbsp;Pessoais</legend>
                    <fieldset class="fieldsetStyle">
                        <legend class="legendFieldset">Documentos</legend>
                        <table>
                            <tr>
                                <td>
                                    <asp:Label ID="Label21" runat="server" Text="CPF" SkinID="labelAzul" Style="margin-left: 3px"></asp:Label>
                                </td>
                                <td>
                                    <telerik:RadMaskedTextBox ID="txtDocumentosCPF" runat="server" Mask="###.###.###-##"
                                        Width="125px" EnableEmbeddedSkins="false" Skin="PortalTelerik" AutoPostBack="True"
                                        OnTextChanged="txtDocumentosCPF_TextChanged">
                                    </telerik:RadMaskedTextBox>
                                    <span class="red">*</span>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label ID="Label22" runat="server" Text="RG" SkinID="labelAzul" Style="margin-left: 3px"></asp:Label>
                                </td>
                                <td>
                                    <%--<telerik:RadNumericTextBox ID="txtDocumentosRG" runat="server" Width="125px" MaxLength="15"
                                    Type="Number" EnableEmbeddedSkins="false" Skin="PortalTelerik" MinValue="0" NumberFormat-DecimalDigits="0"
                                    NumberFormat-GroupSeparator="">
                                </telerik:RadNumericTextBox>--%>
                                    <telerik:RadTextBox ID="txtDocumentosRG" runat="server" Width="125px" MaxLength="15"
                                        EnableEmbeddedSkins="false" Skin="PortalTelerik">
                                    </telerik:RadTextBox>
                                    <span class="red">*</span>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label ID="Label23" runat="server" Text="Órgão Emissor" SkinID="labelAzul" Style="margin-left: 3px"></asp:Label>
                                </td>
                                <td>
                                    <telerik:RadTextBox ID="txtDocumentosRGOrgEmissor" runat="server" Width="60px" EnableEmbeddedSkins="false"
                                        Skin="PortalTelerik" MaxLength="13">
                                    </telerik:RadTextBox>
                                    <span class="red">*</span>
                                    <telerik:RadComboBox ID="ddlDocumentosRGOrgEmissorUF" runat="server" MarkFirstMatch="True"
                                        EnableEmbeddedSkins="false" Skin="PortalTelerik" AppendDataBoundItems="true"
                                        Width="50px" Height="200px">
                                    </telerik:RadComboBox>
                                    <span class="red">*</span>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label ID="Label24" runat="server" Text="Data de Expedição" SkinID="labelAzul"
                                        Style="margin-left: 3px; margin-right: 5px;"></asp:Label>
                                </td>
                                <td>
                                    <telerik:RadDatePicker ID="dpDocumentosRGDataExpedicao" runat="server" Width="130px"
                                        EnableEmbeddedSkins="false" Skin="PortalTelerik" MinDate="01/01/1900">
                                    </telerik:RadDatePicker>
                                    <span class="red">*</span>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label ID="Label25" runat="server" Text="Título de Eleitor" SkinID="labelAzul"
                                        Style="margin-left: 3px"></asp:Label>
                                </td>
                                <td>
                                    <telerik:RadMaskedTextBox ID="txtDocumentosTituloEleitorNumero" runat="server" Mask="############"
                                        Width="125px" Style="margin-left: 0px" EnableEmbeddedSkins="false" Skin="PortalTelerik">
                                    </telerik:RadMaskedTextBox>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label ID="Label26" runat="server" Text="Zona" SkinID="labelAzul" Style="margin-left: 3px"></asp:Label>
                                </td>
                                <td>
                                    <telerik:RadMaskedTextBox ID="txtDocumentosTituloEleitorZona" runat="server" Mask="####"
                                        Width="125px" Style="margin-left: 0px" EnableEmbeddedSkins="false" Skin="PortalTelerik">
                                    </telerik:RadMaskedTextBox>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label ID="Label27" runat="server" Text="Cidade do Título" SkinID="labelAzul"
                                        Style="margin-left: 3px; margin-right: 5px;"></asp:Label>
                                </td>
                                <td>
                                    <telerik:RadComboBox Width="50px" Height="200px" MarkFirstMatch="True" ID="ddlDocumentosTituloEleitorUF"
                                        runat="server" AppendDataBoundItems="true" EnableEmbeddedSkins="false" Skin="PortalTelerik"
                                        AutoPostBack="true" OnSelectedIndexChanged="ddlDocumentosTituloEleitorUF_SelectedIndexChanged">
                                    </telerik:RadComboBox>
                                    &nbsp;
                               
                                    <telerik:RadComboBox ID="ddlDocumentosTituloEleitorCidade" runat="server" AppendDataBoundItems="true"
                                        Enabled="False" Width="200px" Height="200px" MarkFirstMatch="True" EnableEmbeddedSkins="false"
                                        OnClientFocus="selecionaTextoDDL" Skin="PortalTelerik">
                                    </telerik:RadComboBox>
                                </td>
                            </tr>
                        </table>
                        <br />
                    </fieldset>
                    <fieldset class="fieldsetStyle">
                        <legend class="legendFieldset">Documento Militar</legend>
                        <table>
                            <tr>
                                <td>
                                    <asp:Label ID="Label28" runat="server" Text="Nº Certf. Militar" SkinID="labelAzul"
                                        Style="margin-left: 3px"></asp:Label>
                                </td>
                                <td>
                                    <asp:TextBox ID="txtDocumentosMilitarNumero" runat="server" Width="110px" MaxLength="20"
                                        EnableEmbeddedSkins="false" Skin="PortalTelerik"></asp:TextBox>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label ID="Label29" runat="server" Text="Série" SkinID="labelAzul" Style="margin-left: 3px"></asp:Label>
                                </td>
                                <td>
                                    <telerik:RadTextBox ID="txtDocumentosMilitarSerie" runat="server" MaxLength="1" Width="118px"
                                        Style="margin-left: 6px;">
                                    </telerik:RadTextBox>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label ID="Label30" runat="server" Text="Complemento" SkinID="labelAzul" Style="margin-left: 3px"></asp:Label>
                                </td>
                                <td>
                                    <telerik:RadTextBox ID="txtDocumentosMilitarComplemento" runat="server" MaxLength="10"
                                        Width="118px" Style="margin-left: 6px;">
                                    </telerik:RadTextBox>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label ID="Label31" runat="server" Text="Situação" SkinID="labelAzul" Style="margin-left: 3px"></asp:Label>
                                </td>
                                <td>
                                    <telerik:RadTextBox ID="txtDocumentosMilitarSituacao" runat="server" MaxLength="30"
                                        Width="118px" Style="margin-left: 6px;">
                                    </telerik:RadTextBox>
                                </td>
                            </tr>
                        </table>
                    </fieldset>
                    <fieldset class="fieldsetStyle">
                        <legend class="legendFieldset">Outros Documentos</legend>
                        <table>
                            <tr>
                                <td>
                                    <asp:Label ID="Label32" runat="server" Text="CPF da Mãe" SkinID="labelAzul" Style="margin-left: 3px"></asp:Label>
                                </td>
                                <td>
                                    <telerik:RadMaskedTextBox ID="txtDocumentosCPFMae" runat="server" Mask="###.###.###-##"
                                        Width="125px" Style="margin-left: 5px" EnableEmbeddedSkins="false" Skin="PortalTelerik">
                                    </telerik:RadMaskedTextBox>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label ID="Label33" runat="server" Text="CPF do Pai" SkinID="labelAzul" Style="margin-left: 3px"></asp:Label>
                                </td>
                                <td>
                                    <telerik:RadMaskedTextBox ID="txtDocumentosCPFPai" runat="server" Mask="###.###.###-##"
                                        Width="125px" Style="margin-left: 5px" EnableEmbeddedSkins="false" Skin="PortalTelerik">
                                    </telerik:RadMaskedTextBox>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label ID="Label34" runat="server" Text="CPF do Resp." SkinID="labelAzul" Style="margin-left: 3px"></asp:Label>
                                </td>
                                <td>
                                    <telerik:RadMaskedTextBox ID="txtDocumentosCPFResp" runat="server" Mask="###.###.###-##"
                                        Width="125px" Style="margin-left: 5px" EnableEmbeddedSkins="false" Skin="PortalTelerik">
                                    </telerik:RadMaskedTextBox>
                                </td>
                            </tr>
                        </table>
                    </fieldset>
                    <br />
                </fieldset>
                <fieldset class="fieldsetStyle" style="width: 700px;" runat="server" id="fsDocumentosPessoaisEstrangeiro">
                    <legend class="legendFieldset">Documentos&nbsp;Pessoais</legend>
                    <fieldset class="fieldsetStyle">
                        <legend class="legendFieldset">Documentos</legend>
                        <table>
                            <tr>
                                <td>
                                    <asp:Label ID="Label35" runat="server" Text="RNE" SkinID="labelAzul" Style="margin-left: 3px"></asp:Label>
                                </td>
                                <td>
                                    <asp:TextBox ID="txtDocumentosRNE" runat="server" MaxLength="9" Width="118"></asp:TextBox>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label ID="Label36" runat="server" Text="Órgão Emissor" SkinID="labelAzul" Style="margin-left: 3px"></asp:Label>
                                </td>
                                <td>
                                    <asp:TextBox ID="txtDocumentosRNEOrgEmissor" runat="server" Width="118" MaxLength="20"></asp:TextBox>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label ID="Label37" runat="server" Text="CPF" SkinID="labelAzul" Style="margin-left: 3px"></asp:Label>
                                </td>
                                <td>
                                    <telerik:RadMaskedTextBox ID="txtDocumentosCPFEstrangeiro" runat="server" Mask="###.###.###-##"
                                        Width="125px" Style="margin-left: 5px" EnableEmbeddedSkins="false" Skin="PortalTelerik">
                                    </telerik:RadMaskedTextBox>
                                </td>
                            </tr>
                        </table>
                        <br />
                    </fieldset>
                    <fieldset class="fieldsetStyle">
                        <legend class="legendFieldset">Dados do Passaporte</legend>
                        <table>
                            <tr>
                                <td>
                                    <asp:Label ID="Label40" runat="server" Text="Nº do Passaporte" SkinID="labelAzul"
                                        Style="margin-left: 3px"></asp:Label>
                                </td>
                                <td>
                                    <asp:TextBox ID="txtDocumentosPassaporteNumero" runat="server" Width="118" MaxLength="15"></asp:TextBox>
                                    <%--<span class="red">*</span>--%>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label ID="Label43" runat="server" Text="Data de Emissão" SkinID="labelAzul"
                                        Style="margin-left: 3px"></asp:Label>
                                </td>
                                <td>
                                    <telerik:RadDatePicker ID="dpDocumentosPassaporteDataEmissao" runat="server" Width="130px"
                                        Style="margin-left: 4px" EnableEmbeddedSkins="false" Skin="PortalTelerik" MinDate="01/01/1900">
                                    </telerik:RadDatePicker>
                                    <%--<span class="red">*</span>--%>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label ID="Label44" runat="server" Text="Data de Validade" SkinID="labelAzul"
                                        Style="margin-left: 3px"></asp:Label>
                                </td>
                                <td>
                                    <telerik:RadDatePicker ID="dpDocumentosPassaporteDataValidade" runat="server" Width="130px"
                                        Style="margin-left: 4px" EnableEmbeddedSkins="false" Skin="PortalTelerik" MinDate="01/01/1900">
                                    </telerik:RadDatePicker>
                                    <%--<span class="red">*</span>--%>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label ID="Label45" runat="server" Text="País de Emissão" SkinID="labelAzul"
                                        Style="margin-left: 3px"></asp:Label>
                                </td>
                                <td>
                                    <asp:TextBox ID="txtDocumentosPassaportePais" runat="server" Width="118" MaxLength="50"></asp:TextBox>
                                    <%--<span class="red">*</span>--%>
                                </td>
                            </tr>
                        </table>
                    </fieldset>
                </fieldset>
                <fieldset class="fieldsetStyle" style="width: 700px;" runat="server">
                    <legend class="legendFieldset">Endereço</legend>
                    <fieldset class="fieldsetStyle">
                        <legend class="legendFieldset">Residencial</legend>
                        <table>
                            <tr>
                                <td>
                                    <asp:Label ID="Label47" runat="server" Text="CEP" SkinID="labelAzul" Style="margin-left: 3px"></asp:Label>
                                </td>
                                <td>
                                    <telerik:RadMaskedTextBox ID="txtEnderecoResidencialCEP" runat="server" Mask="#####-###"
                                                              EnableEmbeddedSkins="false" Skin="PortalTelerik" Width="150px" Style="margin-left: 3px">
                                    </telerik:RadMaskedTextBox>
                                    <span class="red">*</span>
                                </td>
                                <td>
                                    <asp:Label ID="Label41" runat="server" Text="Número" SkinID="labelAzul" Style="margin-left: 3px"></asp:Label>
                                </td>
                                <td>
                                    <telerik:RadNumericTextBox ID="txtEnderecoResidencialNumero" runat="server" EnableEmbeddedSkins="false"
                                                               Skin="PortalTelerik" MaxLength="6" Width="150px" Style="margin-left: 3px;">
                                        <IncrementSettings InterceptArrowKeys="False" InterceptMouseWheel="False" />
                                        <NumberFormat DecimalDigits="0" GroupSeparator="" GroupSizes="9" ZeroPattern="n" />
                                    </telerik:RadNumericTextBox>
                                    <span class="red">*</span>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label ID="Label46" runat="server" Text="Bairro" SkinID="labelAzul" Style="margin-left: 3px"></asp:Label>
                                </td>
                                <td>
                                    <telerik:RadTextBox ID="txtEnderecoResidencialBairro" runat="server" MaxLength="150"
                                                        Width="150px" Style="margin-left: 5px">
                                    </telerik:RadTextBox>
                                    <span class="red">*</span>
                                </td>
                                <td>
                                    <asp:Label ID="Label42" runat="server" Text="Complemento" SkinID="labelAzul" Style="margin-left: 3px"></asp:Label>
                                </td>
                                <td>
                                    <telerik:RadTextBox ID="txtEnderecoResidencialComplemento" runat="server" MaxLength="150"
                                        Width="150px" Style="margin-left: 3px">
                                    </telerik:RadTextBox>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label ID="Label39" runat="server" Text="Logradouro" SkinID="labelAzul" Style="margin-left: 3px"></asp:Label>
                                </td>
                                <td colspan="4">
                                    <asp:TextBox ID="txtEnderecoResidencialLogradouro" runat="server" Width="400px" MaxLength="400"></asp:TextBox>
                                    <span class="red">*</span>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label ID="Label48" runat="server" Text="Estado / Cidade" SkinID="labelAzul"
                                        Style="margin-left: 3px"></asp:Label>
                                </td>
                                <td colspan="4">
                                    <telerik:RadComboBox MarkFirstMatch="True" ID="ddlEnderecoResidencialUF" runat="server"
                                        Style="margin-left: 5px;" EnableEmbeddedSkins="false" Skin="PortalTelerik" AppendDataBoundItems="true"
                                        Width="50px" Height="200px" AutoPostBack="true" OnSelectedIndexChanged="ddlEnderecoResidencialUF_SelectedIndexChanged" 
                                        OnClientLoad="setupAutocomplete">
                                    </telerik:RadComboBox>
                                    <span class="red">*</span> &nbsp;
                               
                                    <telerik:RadComboBox ID="ddlEnderecoResidencialCidade" runat="server" AppendDataBoundItems="true"
                                        Enabled="False" Width="200px" Height="200px" MarkFirstMatch="True" EnableEmbeddedSkins="false"
                                        OnClientFocus="selecionaTextoDDL" Skin="PortalTelerik" OnClientLoad="setupAutocomplete">
                                    </telerik:RadComboBox>
                                    <span class="red">*</span>
                                </td>
                            </tr>
                        </table>
                    </fieldset>
                    <br />
                    <center>
                        <div runat="server" id="divEnderecoTipoCorrespondencia">
                            <asp:RadioButtonList ID="rblEnderecoTipoCorrespondencia" runat="server" RepeatDirection="Horizontal"
                                AutoPostBack="true" OnSelectedIndexChanged="rblEnderecoTipoCorrespondencia_SelectedIndexChanged">
                            </asp:RadioButtonList>
                        </div>
                    </center>
                    <fieldset class="fieldsetStyle" runat="server" id="fsEnderecoTipoCorrespondenciaBrasil">
                        <legend class="legendFieldset">Correspondência</legend>
                        <table>
                            <tr>
                                <td>
                                    <asp:Label ID="Label49" runat="server" Text="Logradouro" SkinID="labelAzul" Style="margin-left: 3px"></asp:Label>
                                </td>
                                <td colspan="4">
                                    <telerik:RadTextBox ID="txtEnderecoCorrespondenciaLogradouro" Width="400px" runat="server"
                                        MaxLength="400" Style="margin-left: 1px;">
                                    </telerik:RadTextBox>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label ID="Label50" runat="server" Text="Número" SkinID="labelAzul" Style="margin-left: 3px"></asp:Label>
                                </td>
                                <td>
                                    <telerik:RadNumericTextBox ID="txtEnderecoCorrespondenciaNumero" runat="server" EnableEmbeddedSkins="false"
                                        Skin="PortalTelerik" MaxLength="6" Width="150px" Style="margin-left: 1px;">
                                        <IncrementSettings InterceptArrowKeys="False" InterceptMouseWheel="False" />
                                        <NumberFormat DecimalDigits="0" GroupSeparator="" GroupSizes="9" ZeroPattern="n" />
                                    </telerik:RadNumericTextBox>
                                </td>
                                <td>
                                    <asp:Label ID="Label51" runat="server" Text="Complemento" SkinID="labelAzul" Style="margin-left: 13px"></asp:Label>
                                </td>
                                <td>
                                    <telerik:RadTextBox ID="txtEnderecoCorrespondenciaComplemento" runat="server" Width="150px"
                                        MaxLength="200">
                                    </telerik:RadTextBox>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label ID="Label52" runat="server" Text="Bairro" SkinID="labelAzul" Style="margin-left: 3px"></asp:Label>
                                </td>
                                <td>
                                    <telerik:RadTextBox ID="txtEnderecoCorrespondenciaBairro" runat="server" MaxLength="150"
                                        Width="150px" Style="margin-left: 1px;">
                                    </telerik:RadTextBox>
                                </td>
                                <td>
                                    <asp:Label ID="Label53" runat="server" Text="CEP" SkinID="labelAzul" Style="margin-left: 13px; margin-right: 5px"></asp:Label>
                                </td>
                                <td>
                                    <telerik:RadMaskedTextBox ID="txtEnderecoCorrespondenciaCEP" runat="server" Mask="#####-###"
                                        EnableEmbeddedSkins="false" Skin="PortalTelerik" Width="150px">
                                    </telerik:RadMaskedTextBox>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label ID="Label54" runat="server" Text="Estado / Cidade" SkinID="labelAzul"
                                        Style="margin-left: 3px; margin-right: 5px;"></asp:Label>
                                </td>
                                <td colspan="3">
                                    <telerik:RadComboBox MarkFirstMatch="True" ID="ddlEnderecoCorrespondenciaUF" runat="server"
                                        EnableEmbeddedSkins="false" Skin="PortalTelerik" AppendDataBoundItems="true"
                                        Style="margin-left: 1px;" Width="50px" Height="200px" AutoPostBack="true" OnSelectedIndexChanged="ddlEnderecoCorrespondenciaUF_SelectedIndexChanged">
                                    </telerik:RadComboBox>
                                    &nbsp;
                               
                                    <telerik:RadComboBox ID="ddlEnderecoCorrespondenciaCidade" runat="server" AppendDataBoundItems="true"
                                        Enabled="False" Width="200px" Height="200px" MarkFirstMatch="True" EnableEmbeddedSkins="false"
                                        OnClientFocus="selecionaTextoDDL" Skin="PortalTelerik">
                                    </telerik:RadComboBox>
                                </td>
                            </tr>
                        </table>
                    </fieldset>
                    <fieldset class="fieldsetStyle" runat="server" id="fsEnderecoTipoCorrespondenciaFORABrasil">
                        <legend class="legendFieldset">Correspondência</legend>
                        <table>
                            <tr>
                                <td>
                                    <asp:Label ID="Label55" runat="server" Text="Endereço" SkinID="labelAzul" Style="margin-left: 3px"></asp:Label>
                                </td>
                                <td>
                                    <telerik:RadTextBox ID="txtEnderecoCorrespondenciaFORAEndereco" runat="server" Width="400px"
                                        MaxLength="400" Style="margin-left: 4px">
                                    </telerik:RadTextBox>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label ID="Label56" runat="server" Text="Estado/Distrito" SkinID="labelAzul"
                                        Style="margin-left: 3px"></asp:Label>
                                </td>
                                <td>
                                    <telerik:RadTextBox ID="txtEnderecoCorrespondenciaFORAEstadoDistrito" runat="server"
                                        Style="margin-left: 4px" Width="400px" MaxLength="150">
                                    </telerik:RadTextBox>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label ID="Label57" runat="server" Text="Cidade" SkinID="labelAzul" Style="margin-left: 3px"></asp:Label>
                                </td>
                                <td>
                                    <telerik:RadTextBox ID="txtEnderecoCorrespondenciaFORACidade" runat="server" Width="400px"
                                        Style="margin-left: 4px" MaxLength="100">
                                    </telerik:RadTextBox>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label ID="Label58" runat="server" Text="Código Postal" SkinID="labelAzul" Style="margin-left: 3px"></asp:Label>
                                </td>
                                <td>
                                    <telerik:RadNumericTextBox ID="txtEnderecoCorrespondenciaFORACodigoPostal" runat="server"
                                        Style="margin-left: 4px" Width="400px" MaxLength="8" Type="Number" EnableEmbeddedSkins="false"
                                        Skin="PortalTelerik" MinValue="0" NumberFormat-DecimalDigits="0" NumberFormat-GroupSeparator="">
                                    </telerik:RadNumericTextBox>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label ID="Label59" runat="server" Text="País" SkinID="labelAzul" Style="margin-left: 3px"></asp:Label>
                                </td>
                                <td>
                                    <telerik:RadComboBox ID="ddlEnderecoCorrespondenciaFORAPais" runat="server" EnableEmbeddedSkins="false"
                                        Style="margin-left: 4px" Skin="PortalTelerik" Width="400px" Height="200px" AppendDataBoundItems="True"
                                        MarkFirstMatch="true">
                                    </telerik:RadComboBox>
                                </td>
                            </tr>
                        </table>
                    </fieldset>
                </fieldset>
                <fieldset class="fieldsetStyle" style="width: 700px;">
                    <legend class="legendFieldset">Informações Escolares e Acadêmicas</legend>
                    <br />
                    <table>
                        <tr>
                            <td>
                                <asp:Label ID="Label60" runat="server" Text="Escola Fim E. Médio" SkinID="labelAzul"
                                    Style="margin-left: 24px"></asp:Label>
                            </td>
                            <td colspan="2">
                                <asp:TextBox ID="txtInformacoesIDEscola" runat="server" Width="51px" MaxLength="5"
                                    Style="margin-left: 0px;" AutoPostBack="True" OnTextChanged="txtInformacoesIDEscola_TextChanged"></asp:TextBox>
                                <asp:TextBox ID="txtInformacoesEscola" runat="server" SkinID="txtMedio" Enabled="false"></asp:TextBox>
                                <span class="red">*</span>

                                &nbsp;
                           
                                <telerik:RadButton ID="btnInformacoesBuscaEscola" runat="server" EnableEmbeddedSkins="false"
                                    Width="15px" Skin="PortalTelerik" Icon-PrimaryIconUrl="~/App_Themes/PortalTelerik/Button/IconePesquisar.png"
                                    OnClick="btnInformacoesBuscaEscola_Click">
                                </telerik:RadButton>
                                <telerik:RadButton ID="btnInformacoesAdicionaEscola" runat="server" EnableEmbeddedSkins="false"
                                    Icon-PrimaryIconWidth="12px" Width="15px" Skin="PortalTelerik" Icon-PrimaryIconUrl="~/App_Themes/PortalTelerik/Button/IconeMais.png"
                                    OnClick="btnInformacoesAdicionaEscola_Click">
                                </telerik:RadButton>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:Label ID="Label61" runat="server" Text="Ano de Conclusão" SkinID="labelAzul"
                                    Style="margin-left: 24px; margin-right: 5px"></asp:Label>
                            </td>
                            <td colspan="2">
                                <telerik:RadNumericTextBox ID="txtInformacoesEscolaAnoConclusao" runat="server" Width="60px"
                                    MaxLength="4" EnableEmbeddedSkins="false" Skin="PortalTelerik" Style="margin-left: 0px;">
                                    <NumberFormat DecimalDigits="0" ZeroPattern="n" GroupSeparator="" GroupSizes="9" />
                                    <IncrementSettings InterceptArrowKeys="False" InterceptMouseWheel="False" />
                                </telerik:RadNumericTextBox>
                                <span class="red">*</span>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:Label ID="Label62" runat="server" Text="IES Vestibular" SkinID="labelAzul" Style="margin-left: 24px">
                                </asp:Label>
                            </td>
                            <td colspan="2">
                                <asp:TextBox ID="txtInformacoesIDIes" runat="server" Width="51px" AutoPostBack="True"
                                    MaxLength="4" OnTextChanged="txtInformacoesIDIes_TextChanged" Style="margin-left: 0px;"></asp:TextBox>
                                <asp:TextBox ID="txtInformacoesIes" runat="server" SkinID="txtMedio" Enabled="false"></asp:TextBox>
                                &nbsp;
                           
                                <telerik:RadButton ID="btnInformacoesBuscaIES" runat="server" EnableEmbeddedSkins="false"
                                    Width="15px" Skin="PortalTelerik" Icon-PrimaryIconUrl="~/App_Themes/PortalTelerik/Button/IconePesquisar.png"
                                    OnClick="btnInformacoesBuscaIES_Click">
                                </telerik:RadButton>
                                <telerik:RadButton ID="btnInformacoesAdicionaIES" runat="server" EnableEmbeddedSkins="false"
                                    Icon-PrimaryIconWidth="12px" Width="15px" Skin="PortalTelerik" Icon-PrimaryIconUrl="~/App_Themes/PortalTelerik/Button/IconeMais.png"
                                    OnClick="btnInformacoesAdicionaIES_Click">
                                </telerik:RadButton>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:Label ID="Label63" runat="server" Text="Ano do Vestibular" SkinID="labelAzul"
                                    Style="margin-left: 24px"></asp:Label>
                            </td>
                            <td colspan="2">
                                <asp:TextBox ID="txtInformacoesAnoVestibular" runat="server" Width="51px" MaxLength="5"
                                    Style="margin-left: 0px;" AutoPostBack="True" OnTextChanged="txtInformacoesAnoVestibular_TextChanged"></asp:TextBox>
                            </td>
                        </tr>
                        <tr>
                            <td valign="top">
                                <asp:Label ID="Label64" runat="server" Text="Disc. Vestibular" SkinID="labelAzul"
                                    Style="margin-left: 24px"></asp:Label>
                            </td>
                            <td colspan="2">
                                <asp:TextBox ID="txtInformacoesDiscVest" runat="server" TextMode="MultiLine" SkinID="txtMultiLine"
                                    Style="font-family: Courier, 'Courier New', Monospace; font-size: 10pt; height: 134px; width: 420px; margin-left: 0px;"
                                    MaxLength="180"></asp:TextBox>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:Label ID="Label65" runat="server" Text="Transferido de" SkinID="labelAzul" Style="margin-left: 24px"></asp:Label>
                            </td>
                            <td colspan="2">
                                <asp:TextBox ID="txtInformacoesIDTransferido" runat="server" SkinID="txtPequeno"
                                    Style="margin-left: 0px;" Width="51px" MaxLength="4" AutoPostBack="True" OnTextChanged="txtInformacoesIDTransferido_TextChanged"></asp:TextBox>
                                <asp:TextBox ID="txtInformacoesTranferido" runat="server" SkinID="txtMedio" Enabled="false"></asp:TextBox>&nbsp;
                           
                                <telerik:RadButton ID="btnInformacoesBuscaTransferido" runat="server" EnableEmbeddedSkins="false"
                                    Width="15px" Skin="PortalTelerik" Icon-PrimaryIconUrl="~/App_Themes/PortalTelerik/Button/IconePesquisar.png"
                                    OnClick="btnInformacoesBuscaTransferido_Click">
                                </telerik:RadButton>
                                <telerik:RadButton ID="btnInformacoesAdicionaTransferido" runat="server" EnableEmbeddedSkins="false"
                                    Icon-PrimaryIconWidth="12px" Width="15px" Skin="PortalTelerik" Icon-PrimaryIconUrl="~/App_Themes/PortalTelerik/Button/IconeMais.png"
                                    OnClick="btnInformacoesAdicionaTransferido_Click">
                                </telerik:RadButton>
                            </td>
                        </tr>
                    </table>
                </fieldset>
                <fieldset class="fieldsetStyle" runat="server" style="width: 700px;">
                    <legend class="legendFieldset">Documentos Pendentes</legend>
                    <br />
                    <center>
                        <div id="divInfo" runat="server" class="aviso" visible="false">
                            <div>
                                <asp:Label runat="server" ID="lblInformacao"></asp:Label>
                            </div>
                        </div>
                        <div id="divDocPendentes" runat="server">
                            <table>
                                <tr>
                                    <td>
                                        <asp:Label ID="Label66" runat="server" Text="Documento" SkinID="labelAzul" Width="100px"></asp:Label>
                                    </td>
                                    <td>
                                        <telerik:RadComboBox ID="ddlDocPendentesPendencias" runat="server" EnableEmbeddedSkins="false"
                                            Skin="PortalTelerik" Width="457px" Height="250" MarkFirstMatch="true" Filter="Contains"
                                            DataTextField="NomeDocumento" DataValueField="ID_Documento" Style="margin-left: 4px;">
                                        </telerik:RadComboBox>
                                    </td>
                                </tr>
                                <tr>
                                    <td>
                                        <asp:Label ID="Label67" runat="server" Text="Observação" SkinID="labelAzul" Width="100px"></asp:Label>
                                    </td>
                                    <td>
                                        <asp:TextBox ID="txtDocPendenteObs" runat="server" Width="450px" MaxLength="100"></asp:TextBox>
                                    </td>
                                </tr>
                                <tr>
                                    <td colspan="2">
                                        <center>
                                            <telerik:RadButton ID="btnAdicionaDocPendente" runat="server" Text="Adicionar Documento"
                                                Skin="PortalTelerik" EnableEmbeddedSkins="false" Style="margin-right: 0" OnClick="btnAdicionaDocPendente_Click">
                                            </telerik:RadButton>
                                        </center>
                                    </td>
                                </tr>
                            </table>
                        </div>
                        <br />
                        <asp:GridView ID="grdDocumentosPendentes" runat="server" AutoGenerateColumns="False"
                            EmptyDataText="Não possui documentos pendentes." OnRowCommand="grdDocumentosPendentes_RowCommand"
                            OnRowDataBound="grdDocumentosPendentes_RowDataBound" Width="660">
                            <Columns>
                                <asp:BoundField AccessibleHeaderText="ID_Documento" DataField="ID_Documento" HeaderText="Cod. Doc."
                                    SortExpression="ID_Documento" Visible="false" />
                                <asp:TemplateField HeaderText="Documento" ItemStyle-Width="300px">
                                    <ItemTemplate>
                                        <asp:Label ID="lblDocumentoNomeGrdDocumentosPendentes" runat="server"></asp:Label>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:BoundField AccessibleHeaderText="Obs" DataField="Obs" HeaderText="Observação"
                                    ItemStyle-Width="300px" SortExpression="Obs" />
                                <asp:TemplateField HeaderText="Excluir">
                                    <ItemTemplate>
                                        <asp:ImageButton ID="imgBtnExcluir" runat="server" ImageUrl="~/images/botoes/btndelete.png"
                                            OnClientClick="return confirm('Deseja realmente excluir este registro?');" CommandArgument='<%# Container.DataItemIndex %>'
                                            ToolTip="Excluir Registro" CommandName="Excluir" />
                                    </ItemTemplate>
                                    <HeaderStyle HorizontalAlign="Center" Width="10%" />
                                    <ItemStyle HorizontalAlign="Center" />
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
                    </center>
                    <br />
                </fieldset>
                <fieldset class="fieldsetStyle" style="width: 700px;">
                    <legend class="legendFieldset">Deficiência</legend>
                    <br />
                    <center>
                        <table>
                            <tr>
                                <td>
                                    <asp:Label ID="Label38" runat="server" Text="Deficiência" SkinID="labelAzul" Width="100px"
                                        Style="margin-right: 5px;"></asp:Label>
                                </td>
                                <td>
                                    <telerik:RadComboBox ID="ddlDeficiencia" runat="server" EnableEmbeddedSkins="false"
                                        Skin="PortalTelerik" Width="450px" Height="250" MarkFirstMatch="true" Filter="Contains"
                                        Culture="pt-BR" DataTextField="Descricao" DataValueField="ID_tipoDeficiencia">
                                    </telerik:RadComboBox>
                                </td>
                            </tr>
                            <tr>
                                <td colspan="2">
                                    <center>
                                        <telerik:RadButton ID="ddlDeficienciaAdicionar" runat="server" Text="Adicionar Deficiência"
                                            Skin="PortalTelerik" EnableEmbeddedSkins="false" Style="margin-right: 0" OnClick="ddlDeficienciaAdicionar_Click">
                                        </telerik:RadButton>
                                    </center>
                                </td>
                            </tr>
                        </table>
                        <br />
                        <asp:GridView ID="grdDeficiencia" runat="server" AutoGenerateColumns="False" EmptyDataText="Não possui deficiência."
                            OnRowCommand="grdDeficiencia_RowCommand" OnRowDataBound="grdDeficiencia_RowDataBound"
                            Width="660px">
                            <Columns>
                                <asp:BoundField AccessibleHeaderText="ID_tipoDeficiencia" DataField="ID_tipoDeficiencia"
                                    HeaderText="ID_tipoDeficiencia" SortExpression="ID_tipoDeficiencia" Visible="false" />
                                <asp:TemplateField HeaderText="Tipo de Deficiência">
                                    <ItemTemplate>
                                        <asp:Label ID="lblTipoDeficienciaGrdDeficiencia" runat="server"></asp:Label>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Excluir">
                                    <ItemTemplate>
                                        <asp:ImageButton ID="imgBtnExcluir" runat="server" ImageUrl="~/images/botoes/btndelete.png"
                                            OnClientClick="return confirm('Deseja realmente excluir este registro?');" CommandArgument='<%# Container.DataItemIndex %>'
                                            ToolTip="Excluir Registro" CommandName="Excluir" />
                                    </ItemTemplate>
                                    <HeaderStyle HorizontalAlign="Center" Width="10%" />
                                    <ItemStyle HorizontalAlign="Center" />
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
                    </center>
                    <br />
                </fieldset>
                <br />
                <table width="100%">
                    <tr>
                        <td align="center">
                            <telerik:RadButton ID="btnSalvar" runat="server" Text="Salvar" Style="margin: 10px 10px 10px 0"
                                EnableEmbeddedSkins="false" Skin="PortalTelerik" OnClientClicked="confirmaSalvar"
                                OnClick="btnSalvar_Click">
                            </telerik:RadButton>
                            <telerik:RadButton ID="btnVoltar" runat="server" Text="Voltar" Style="margin: 10px 10px 10px 0"
                                EnableEmbeddedSkins="false" Skin="PortalTelerik" CausesValidation="false" OnClick="btnVoltar_Click">
                            </telerik:RadButton>
                            <asp:Button ID="btnVoltaPaginaConsultaAcademico" runat="server" OnClick="btnVoltaPaginaConsultaAcademico_Click"
                                Visible="false" />
                        </td>
                    </tr>
                </table>
                <br />
                <asp:Button ID="btnCarregarPessoaExistente" runat="server" OnClick="btnCarregarPessoaExistente_Click"
                    Visible="false" />
                <div id="modalAviso" class="modal_padrao" style="width: 400px; height: 150px; margin: -75px auto auto -200px !important;">
                    <div id="fecharModalAviso" onclick="fecharModal3">
                        <img src="../images/botoes/fecha_popup.png" alt="X" />
                    </div>
                    <h2 style="margin-left: 15px; font-size: 20px;">&nbsp;Aviso "> &nbsp;Aviso
                    </h2>
                    <center>
                        <asp:Label ID="lblModalAviso" runat="server" Text=""></asp:Label>
                        <telerik:RadButton ID="btnContMat" runat="server" Text="Continuar Matrícula" Style="margin: 10px 10px 10px 0"
                            EnableEmbeddedSkins="false" Skin="PortalTelerik" Visible="false" CausesValidation="false">
                        </telerik:RadButton>
                    </center>
                </div>
                <telerik:RadWindowManager ID="windowManager" runat="server" EnableShadow="true" EnableEmbeddedSkins="false"
                    Skin="PortalTelerik">
                </telerik:RadWindowManager>
                <telerik:RadToolTip ID="RadToolTipModalBusca" runat="server" EnableShadow="True"
                    Modal="True" Position="Center" RelativeTo="BrowserWindow" IsClientID="true" Title=""
                    Width="800px" Height="370px" OnClientShow="ClientShow" ShowEvent="FromCode" HideEvent="ManualClose"
                    HideDelay="0" ShowDelay="0" RenderInPageRoot="true" EnableEmbeddedSkins="false"
                    Skin="PortalTelerik">
                    <table>
                        <tr>
                            <td>
                                <asp:TextBox ID="txtModalBusca" runat="server"></asp:TextBox>
                            </td>
                            <td>
                                <telerik:RadButton ID="btnModalBusca" runat="server" EnableEmbeddedSkins="false"
                                    Width="15px" Skin="PortalTelerik" Icon-PrimaryIconUrl="~/App_Themes/PortalTelerik/Button/IconePesquisar.png"
                                    OnClick="btnModalBusca_Click">
                                </telerik:RadButton>
                            </td>
                        </tr>
                    </table>
                    <asp:GridView ID="gvEscolas" runat="server" AllowPaging="true" PageSize="10" Width="100%"
                        AutoGenerateColumns="false" SkinID="GridSIIA" OnPageIndexChanging="gvEscolas_PageIndexChanging"
                        OnRowCommand="gvEscolas_RowCommand">
                        <Columns>
                            <asp:TemplateField HeaderText="Selec.">
                                <ItemTemplate>
                                    <asp:ImageButton runat="server" ID="btnSelecionaGrid" CommandName="selEscola" CommandArgument='<%#Eval("c_escola") + "|" + Eval("nome_escola")%>'
                                        AlternateText="Selecione" ToolTip="Selecione" ImageUrl="~/images/selecionar.gif" />
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Cod.">
                                <ItemTemplate>
                                    <asp:LinkButton ID="lbCodEscola" runat="server" CommandName="selEscola" Text='<%#Eval("c_escola")%>'
                                        CommandArgument='<%#Eval("c_escola") + "|" + Eval("nome_escola")%>'></asp:LinkButton>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Escola">
                                <ItemTemplate>
                                    <asp:LinkButton ID="lbEscola" runat="server" CommandName="selEscola" Text='<%#Eval("nome_escola")%>'
                                        CommandArgument='<%#Eval("c_escola") + "|" + Eval("nome_escola")%>'></asp:LinkButton>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Cidade">
                                <ItemTemplate>
                                    <asp:LinkButton ID="lbCidade" runat="server" CommandName="selEscola" Text='<%#Eval("cidadeestado")%>'
                                        CommandArgument='<%#Eval("c_escola") + "|" + Eval("nome_escola")%>'></asp:LinkButton>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Editar">
                                <ItemTemplate>
                                    <asp:ImageButton runat="server" ID="btnEditarGrid" CommandName="editEscola" CommandArgument='<%#Eval("c_escola") + "|" + Eval("nome_escola")%>'
                                        AlternateText="Editar" ToolTip="Editar" ImageUrl="~/images/botoes/button_editar_gridview.png" />
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>
                    <asp:GridView ID="gvEscolasIES" runat="server" AllowPaging="true" PageSize="10" Width="100%"
                        AutoGenerateColumns="false" SkinID="GridSIIA" OnPageIndexChanging="gvEscolasIES_PageIndexChanging"
                        OnRowCommand="gvEscolasIES_RowCommand">
                        <Columns>
                            <asp:TemplateField HeaderText="Selec.">
                                <ItemTemplate>
                                    <asp:ImageButton runat="server" ID="btnSelecionaGrid" CommandName="selEscola" CommandArgument='<%#Eval("ident_escola") + "|" + Eval("nome_escola")%>'
                                        AlternateText="Selecione" ToolTip="Selecione" ImageUrl="~/images/selecionar.gif" />
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Cod.">
                                <ItemTemplate>
                                    <asp:LinkButton ID="lbCodEscola" runat="server" Text='<%#Eval("ident_escola")%>'
                                        CommandName="selEscola" CommandArgument='<%#Eval("ident_escola") + "|" + Eval("nome_escola")%>'></asp:LinkButton>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Escola">
                                <ItemTemplate>
                                    <asp:LinkButton ID="lbEscola" runat="server" CommandName="selEscola" Text='<%#Eval("nome_escola")%>'
                                        CommandArgument='<%#Eval("ident_escola") + "|" + Eval("nome_escola")%>'></asp:LinkButton>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Cidade">
                                <ItemTemplate>
                                    <asp:LinkButton ID="lbCidade" runat="server" CommandName="selEscola" Text='<%#Eval("cidadeestado")%>'
                                        CommandArgument='<%#Eval("ident_escola") + "|" + Eval("nome_escola")%>'></asp:LinkButton>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Editar">
                                <ItemTemplate>
                                    <asp:ImageButton runat="server" ID="btnEditarGrid" CommandName="editEscola" CommandArgument='<%#Eval("ident_escola") + "|" + Eval("nome_escola")%>'
                                        AlternateText="Editar" ToolTip="Editar" ImageUrl="~/images/botoes/button_editar_gridview.png" />
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>
                    <asp:GridView ID="gvTranferido" runat="server" AllowPaging="true" PageSize="10" Width="100%"
                        AutoGenerateColumns="false" SkinID="GridSIIA" OnPageIndexChanging="gvTranferido_PageIndexChanging"
                        OnRowCommand="gvTranferido_RowCommand">
                        <Columns>
                            <asp:TemplateField HeaderText="Selec.">
                                <ItemTemplate>
                                    <asp:ImageButton runat="server" ID="btnSelecionaGrid" CommandName="selEscola" CommandArgument='<%#Eval("ident_escola") + "|" + Eval("nome_escola")%>'
                                        AlternateText="Selecione" ToolTip="Selecione" ImageUrl="~/images/selecionar.gif" />
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Cod.">
                                <ItemTemplate>
                                    <asp:LinkButton ID="lbCodEscola" runat="server" CommandName="selEscola" Text='<%#Eval("ident_escola")%>'
                                        CommandArgument='<%#Eval("ident_escola") + "|" + Eval("nome_escola")%>'></asp:LinkButton>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Escola">
                                <ItemTemplate>
                                    <asp:LinkButton ID="lbEscola" runat="server" Text='<%#Eval("nome_escola")%>' CommandName="selEscola"
                                        CommandArgument='<%#Eval("ident_escola") + "|" + Eval("nome_escola")%>'></asp:LinkButton>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Cidade">
                                <ItemTemplate>
                                    <asp:LinkButton ID="lbCidade" runat="server" CommandName="selEscola" Text='<%#Eval("cidadeestado")%>'
                                        CommandArgument='<%#Eval("ident_escola") + "|" + Eval("nome_escola")%>'></asp:LinkButton>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Editar">
                                <ItemTemplate>
                                    <asp:ImageButton runat="server" ID="btnEditarGrid" CommandName="editEscola" CommandArgument='<%#Eval("ident_escola") + "|" + Eval("nome_escola")%>'
                                        AlternateText="Editar" ToolTip="Editar" ImageUrl="~/images/botoes/button_editar_gridview.png" />
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>
                </telerik:RadToolTip>
                <telerik:RadToolTip ID="RadToolTipModalCadastroEscolaIesOuTransf" runat="server"
                    EnableShadow="True" Modal="True" Position="Center" RelativeTo="BrowserWindow"
                    IsClientID="true" Title="" Width="500px" Height="300px" OnClientShow="ClientShow"
                    ShowEvent="FromCode" HideEvent="ManualClose" HideDelay="0" ShowDelay="0" RenderInPageRoot="true"
                    EnableEmbeddedSkins="false" Skin="PortalTelerik">
                    <asp:HiddenField ID="hfIdent_escola" runat="server" />
                    <asp:HiddenField ID="hfIesOuTransf" runat="server" />
                    <div id="dvHistescola" runat="server" style="margin: 5px 5px 0 5px;">
                        <table>
                            <tr>
                                <td>
                                    <asp:Label ID="lblCadEscINome" runat="server" Text="Nome" Width="120px" Style="text-align: right;"
                                        SkinID="labelAzul"></asp:Label>
                                    <asp:TextBox ID="txtCadEscINome" runat="server" CssClass="inputText Cmp_Form" Width="250px"
                                        MaxLength="60"></asp:TextBox><span class="red" style="margin-right: 3px">*</span>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label ID="lblCadEscIGrau" runat="server" Text="Grau Escolar" Width="120px" Style="text-align: right;"
                                        SkinID="labelAzul"></asp:Label>
                                    <asp:DropDownList ID="ddlCadEscIGrau" runat="server" CssClass="select Text_Table"
                                        Enabled="false" Width="257">
                                        <asp:ListItem Text="3" Value="3"></asp:ListItem>
                                        <asp:ListItem Text="2" Value="2"></asp:ListItem>
                                        <asp:ListItem Text="1" Value="1"></asp:ListItem>
                                    </asp:DropDownList>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label ID="lblCadEscIDiretor" runat="server" Text="Diretor" Width="120px" Style="text-align: right;"
                                        SkinID="labelAzul"></asp:Label>
                                    <asp:TextBox ID="txtCadEscIDiretor" runat="server" CssClass="inputText Cmp_Form"
                                        Width="250px" MaxLength="30"></asp:TextBox>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label ID="lblCadEscISecret" runat="server" Text="Secretario" Width="120px" Style="text-align: right;"
                                        SkinID="labelAzul"></asp:Label>
                                    <asp:TextBox ID="txtCadEscISecret" runat="server" CssClass="inputText Cmp_Form" MaxLength="30"
                                        Width="250px"></asp:TextBox>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label ID="lblCadEscICidade" runat="server" Text="Cidade" Width="120px" Style="text-align: right;"
                                        SkinID="labelAzul"></asp:Label>
                                    <asp:DropDownList ID="ddlCadEscIEstado" runat="server" CssClass="select Text_Table">
                                    </asp:DropDownList>
                                    <asp:DropDownList ID="ddlCadEscICidade" runat="server" CssClass="select Text_Table"
                                        Width="205px">
                                    </asp:DropDownList>
                                    <span class="red" style="margin-right: 3px">*</span>
                                    <ajax:CascadingDropDown ID="ccCadEscIEstado" runat="server" Category="Estado" ServiceMethod="GetDropDownEstadosBrasil"
                                        ServicePath="~/SII/wsLocalidades.asmx" TargetControlID="ddlCadEscIEstado" LoadingText="[Carregando...]">
                                    </ajax:CascadingDropDown>
                                    <ajax:CascadingDropDown ID="ccCadEscICidade" runat="server" Category="Cidade" ServiceMethod="GetDropDownCidadeEstado"
                                        ParentControlID="ddlCadEscIEstado" ServicePath="~/SII/wsLocalidades.asmx" TargetControlID="ddlCadEscICidade"
                                        LoadingText="[Carregando...]">
                                    </ajax:CascadingDropDown>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label ID="lblCadEscIDepAdm" runat="server" Text="Dep. Administrativa" Width="120px"
                                        Style="text-align: right;" SkinID="labelAzul"></asp:Label>
                                    <asp:DropDownList ID="ddlCadEscIDepAdm" runat="server" CssClass="select Text_Table"
                                        Width="257">
                                        <asp:ListItem Text="Particular" Value="P"></asp:ListItem>
                                        <asp:ListItem Text="Federal" Value="F"></asp:ListItem>
                                        <asp:ListItem Text="Estadual" Value="E"></asp:ListItem>
                                        <asp:ListItem Text="Municipal" Value="M"></asp:ListItem>
                                    </asp:DropDownList>
                                </td>
                            </tr>
                            <tr>
                                <td align="center">
                                    <telerik:RadButton ID="btnModalCadastroEscolaIesOuTransfSalvar" runat="server" Text="Salvar"
                                        CssClass="Btn_Form" EnableEmbeddedSkins="false" Skin="PortalTelerik" OnClick="btnModalCadastroEscolaIesOuTransfSalvar_Click" />
                                    <telerik:RadButton ID="btnModalCadastroEscolaIesOuTransfCancelar" runat="server"
                                        Text="Cancelar" CssClass="Btn_Form" EnableEmbeddedSkins="false" Skin="PortalTelerik"
                                        OnClick="btnModalCadastroEscolaIesOuTransfCancelar_Click" />
                                </td>
                            </tr>
                        </table>
                        <br clear="left" />
                        <div>
                            <asp:Label ID="lblErroIES" runat="server" Text="" ForeColor="Red" Font-Bold="true"></asp:Label>
                        </div>
                    </div>
                </telerik:RadToolTip>
                <telerik:RadToolTip ID="RadToolTipModalCadastroEscola" runat="server" EnableShadow="True"
                    Modal="True" Position="Center" RelativeTo="BrowserWindow" IsClientID="true" Title="Cadastro - Escola"
                    Width="638px" Height="350px" OnClientShow="ClientShow" ShowEvent="FromCode" HideEvent="ManualClose"
                    HideDelay="0" ShowDelay="0" RenderInPageRoot="true" EnableEmbeddedSkins="false"
                    Skin="PortalTelerik">
                    <asp:HiddenField ID="hfC_Escola" runat="server" />
                    <table id="Table1" class="tablePaseescolas" runat="server">
                        <tr>
                            <td>
                                <span>
                                    <telerik:RadButton ID="gnEscolaridadeBrasil" runat="server" ToggleType="Radio" ButtonType="ToggleButton"
                                        GroupName="Radios" AutoPostBack="true" EnableAjaxSkinRendering="true" OnClick="gnEscolaridadeBrasil_Click"
                                        Checked="true" Style="cursor: pointer;">
                                        <ToggleStates>
                                            <telerik:RadButtonToggleState Text="BRASIL"></telerik:RadButtonToggleState>
                                            <telerik:RadButtonToggleState Text="BRASIL"></telerik:RadButtonToggleState>
                                        </ToggleStates>
                                    </telerik:RadButton>
                                </span><span style="padding-left: 20px;">
                                    <telerik:RadButton ID="gnEscolaridadeEstrangeiro" runat="server" ToggleType="Radio"
                                        ButtonType="ToggleButton" GroupName="Radios" AutoPostBack="true" EnableAjaxSkinRendering="true"
                                        OnClick="gnEscolaridadeEstrangeiro_Click" Style="cursor: pointer;">
                                        <ToggleStates>
                                            <telerik:RadButtonToggleState Text="ESTRANGEIRO"></telerik:RadButtonToggleState>
                                            <telerik:RadButtonToggleState Text="ESTRANGEIRO"></telerik:RadButtonToggleState>
                                        </ToggleStates>
                                    </telerik:RadButton>
                                </span>
                            </td>
                        </tr>
                        <tr>
                            <td colspan="3">
                                <asp:Label ID="lblCadEscPNome" runat="server" Text="Nome" Width="120px" Style="text-align: right;"
                                    SkinID="labelAzul"> </asp:Label>
                                <asp:TextBox ID="txtCadEscPNome" runat="server" Width="424px" CssClass="inputText Cmp_Form"
                                    MaxLength="40"></asp:TextBox>
                                <span class="red" style="margin-right: 3px">*</span>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:Label ID="lblCadEscPNivel" runat="server" Text="Nivel" Width="120px" Style="text-align: right;"
                                    SkinID="labelAzul"></asp:Label>
                                <asp:DropDownList ID="ddlCadEscPNivel" runat="server" CssClass="select Text_Table"
                                    Width="158px">
                                    <asp:ListItem Text="Selecione" Value="-1" Selected="True"></asp:ListItem>
                                    <asp:ListItem Text="Educação Inferior" Value="0"></asp:ListItem>
                                    <asp:ListItem Text="Ensino Fundamental" Value="1"></asp:ListItem>
                                    <asp:ListItem Text="Ensino Médio" Value="2"></asp:ListItem>
                                    <asp:ListItem Text="Superior" Value="3"></asp:ListItem>
                                </asp:DropDownList>
                            </td>
                            <td style="width: 10px;"></td>
                            <td>
                                <asp:Label ID="lblCadEscPDepAdm" runat="server" Text="Dep. Administrativa" Width="120px"
                                    Style="text-align: right;" SkinID="labelAzul"></asp:Label>
                                <asp:DropDownList ID="ddlCadEscPDepAdm" runat="server" CssClass="select Text_Table"
                                    Width="108">
                                    <asp:ListItem Text="Selecione" Value="-1" Selected="True"></asp:ListItem>
                                    <asp:ListItem Text="Particular" Value="P"></asp:ListItem>
                                    <asp:ListItem Text="Federal" Value="F"></asp:ListItem>
                                    <asp:ListItem Text="Estadual" Value="E"></asp:ListItem>
                                    <asp:ListItem Text="Municipal" Value="M"></asp:ListItem>
                                </asp:DropDownList>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:Label ID="lblCadEscPEndereco" runat="server" Text="Endereco" Width="120px" Style="text-align: right;"
                                    SkinID="labelAzul"></asp:Label>
                                <asp:TextBox ID="txtCadEscPEndereco" runat="server" CssClass="inputText Cmp_Form"
                                    MaxLength="40" Width="150px"></asp:TextBox>
                            </td>
                            <td></td>
                            <td>
                                <asp:Label ID="lblCadEscPNumero" runat="server" Text="Nº" Width="120px" Style="text-align: right;"
                                    SkinID="labelAzul"></asp:Label>
                                <asp:TextBox ID="txtCadEscPNumero" runat="server" CssClass="inputText Cmp_Form" Width="100px"
                                    MaxLength="6"></asp:TextBox>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:Label ID="lblCadEscPComplemento" runat="server" Width="120px" Text="Complemento"
                                    SkinID="labelAzul"></asp:Label>
                                <asp:TextBox ID="txtCadEscPCompl" runat="server" CssClass="inputText Cmp_Form" Width="150px"
                                    MaxLength="20"></asp:TextBox>
                            </td>
                            <td></td>
                            <td>
                                <asp:Label ID="lblCadEscPBairro" runat="server" Text="Bairro" SkinID="labelAzul"
                                    Width="120px"></asp:Label>
                                <asp:TextBox ID="txtCadEscPBairro" runat="server" CssClass="inputText Cmp_Form" Width="100px"
                                    MaxLength="25"></asp:TextBox>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:Label ID="lblCadEscPCep" runat="server" Text="CEP" Width="120px" Style="text-align: right;"
                                    SkinID="labelAzul"></asp:Label>
                                <asp:TextBox ID="txtCadEscPCep" runat="server" CssClass="inputText Cmp_Form" Width="150px"
                                    MaxLength="8"></asp:TextBox>
                            </td>
                            <td></td>
                            <td>
                                <asp:Label ID="lblCadEscPTel" runat="server" Text="Tel" Width="120px" Style="text-align: right;"
                                    SkinID="labelAzul"></asp:Label>
                                <asp:TextBox ID="txtCadEscPTel" runat="server" CssClass="inputText Cmp_Form" Width="100px"
                                    MaxLength="8"></asp:TextBox>
                            </td>
                        </tr>
                        <tr id="CampoBrasil" runat="server" visible="true">
                            <td colspan="3">
                                <asp:Label ID="lblCadEscPCidade" runat="server" Text="Cidade" Width="120px" Style="text-align: right;"
                                    SkinID="labelAzul"></asp:Label>
                                <asp:DropDownList ID="ddlCadEscPEstado" runat="server" CssClass="select Text_Table">
                                </asp:DropDownList>
                                <asp:DropDownList ID="ddlCadEscPCidade" runat="server" CssClass="select Text_Table"
                                    Width="380px">
                                </asp:DropDownList>
                                <span style="margin-left: 3px; color: Red;">*</span>
                                <ajax:CascadingDropDown ID="ccCadEscPEstado" runat="server" Category="Estado" ServiceMethod="GetDropDownEstadosBrasil"
                                    ServicePath="~/SII/wsLocalidades.asmx" TargetControlID="ddlCadEscPEstado" LoadingText="[Carregando...]">
                                </ajax:CascadingDropDown>
                                <ajax:CascadingDropDown ID="ccCadEscPCidade" runat="server" Category="Cidade" ServiceMethod="GetDropDownCidadeEstado"
                                    ParentControlID="ddlCadEscPEstado" ServicePath="~/SII/wsLocalidades.asmx" TargetControlID="ddlCadEscPCidade"
                                    LoadingText="[Carregando...]">
                                </ajax:CascadingDropDown>
                            </td>
                        </tr>
                        <tr id="CampoEstrangeiroPais" runat="server" visible="false">
                            <td colspan="4">
                                <asp:Label ID="Label68" runat="server" Text="País" Width="120px" Style="text-align: right;"
                                    SkinID="labelAzul"></asp:Label>
                                <asp:DropDownList ID="ddlCadEscPPaisEstran" Width="433px" runat="server" CssClass="select Text_Table">
                                </asp:DropDownList>
                                <span style="margin-left: 3px; color: Red;">*</span>
                                <ajax:CascadingDropDown ID="ccCadEscPPais" runat="server" Category="Pais" ServiceMethod="GetDropDownPaisSemBrasil"
                                    ServicePath="~/SII/wsLocalidades.asmx" TargetControlID="ddlCadEscPPaisEstran"
                                    LoadingText="[Carregando...]">
                                </ajax:CascadingDropDown>
                            </td>
                        </tr>
                        <tr id="CampoEstrangeiroDistrito" runat="server" visible="false">
                            <td colspan="4">
                                <asp:Label ID="lblCadEscPDistritoEstrang" runat="server" Text="Estado / Distrito"
                                    Width="120px" Style="text-align: right;" SkinID="labelAzul"></asp:Label>
                                <asp:TextBox ID="txtCadEscPDistritoEstrang" runat="server" CssClass="inputText Cmp_Form"
                                    Width="425px" MaxLength="8">
                                </asp:TextBox>
                            </td>
                        </tr>
                        <tr id="CampoEstrangeiroCidade" runat="server" visible="false">
                            <td colspan="4">
                                <asp:Label ID="lblCadEscPCidadeEstrang" runat="server" Text="Cidade" Width="120px"
                                    Style="text-align: right;" SkinID="labelAzul"></asp:Label>
                                <asp:TextBox ID="txtCadEscPCidadeEstrang" runat="server" CssClass="inputText Cmp_Form"
                                    Width="425px" MaxLength="8"></asp:TextBox>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:Label ID="lblCadEscPConveni" runat="server" Text="Escola Conveniada" Width="120px"
                                    Style="text-align: right;" SkinID="labelAzul"></asp:Label>
                                <asp:DropDownList ID="ddlCadEscPConveni" runat="server" CssClass="select Text_Table"
                                    Width="158px">
                                    <asp:ListItem Text="Selecione" Value="-1" Selected="True"></asp:ListItem>
                                    <asp:ListItem Text="Conveniada" Value="C"></asp:ListItem>
                                    <asp:ListItem Text="Não Conveniada" Value="N"></asp:ListItem>
                                </asp:DropDownList>
                            </td>
                            <td></td>
                            <td>
                                <asp:Label ID="lblCadEscPEmail" runat="server" Width="120px" Text="Email" SkinID="labelAzul"></asp:Label>
                                <asp:TextBox ID="txtCadEscPEmail" runat="server" MaxLength="40" CssClass="inputText Cmp_Form"
                                    Width="100px"></asp:TextBox>
                            </td>
                        </tr>
                        <tr>
                            <td colspan="3">&nbsp;
                            </td>
                        </tr>
                        <tr>
                            <td colspan="3" style="text-align: center">
                                <telerik:RadButton ID="btnModalCadastroEscolaSalvar" runat="server" Text="Salvar"
                                    CssClass="Btn_Form" EnableEmbeddedSkins="false" Skin="PortalTelerik" OnClick="btnModalCadastroEscolaSalvar_Click" />
                                &nbsp;
                           
                                <telerik:RadButton ID="btnEditarEndP" runat="server" Text="Editar" CssClass="Btn_Form"
                                    Visible="false" EnableEmbeddedSkins="false" Skin="PortalTelerik" />
                                <telerik:RadButton ID="btnModalCadastroEscolaCancelar" runat="server" Text="Cancelar"
                                    CssClass="Btn_Form" EnableEmbeddedSkins="false" Skin="PortalTelerik" OnClick="btnModalCadastroEscolaCancelar_Click" />
                            </td>
                        </tr>
                        <tr>
                            <td colspan="2">
                                <asp:Label ID="lblErroCadastroEscP" runat="server" Text="" ForeColor="Red" Font-Bold="true"></asp:Label>
                            </td>
                        </tr>
                    </table>
                </telerik:RadToolTip>
            </div>
        </telerik:RadAjaxPanel>
        <telerik:RadAjaxManager ID="RadAjaxManager1" runat="server">
            <AjaxSettings>
                <telerik:AjaxSetting AjaxControlID="RadAjaxPanel1">
                    <UpdatedControls>
                        <telerik:AjaxUpdatedControl ControlID="content" LoadingPanelID="RadAjaxLoadingPanel1" />
                    </UpdatedControls>
                </telerik:AjaxSetting>
            </AjaxSettings>
        </telerik:RadAjaxManager>
        <telerik:RadAjaxLoadingPanel ID="RadAjaxLoadingPanel1" runat="server" Skin="Windows7"
            IsSticky="True" Style="position: fixed; top: 0; right: 0; bottom: 0; left: 0; height: 100%; width: 100%; margin: 0; padding: 0; z-index: 100"
            Transparency="0">
        </telerik:RadAjaxLoadingPanel>
    </form>
</body>
</html>
