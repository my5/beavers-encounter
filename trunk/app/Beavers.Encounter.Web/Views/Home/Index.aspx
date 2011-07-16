<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" AutoEventWireup="true" 
    Inherits="System.Web.Mvc.ViewPage" %>

<asp:Content ID="indexContent" ContentPlaceHolderID="MainContentPlaceHolder" runat="server">
    <h2><%= Convert.ToString(Application["AppTitle"]) %></h2>
    <h3>Что дальше?</h3>
    <%
    if (!((User) User).Identity.IsAuthenticated) 
    { %>
    <p>
        Для входа в систему перейдите по ссылке <%= Html.ActionLink<AccountController>(c => c.LogOn(), "Войти") %> в верхнем правом углу страницы.
    </p>
    <p>
        Если вы еще не зарегистрированны, то зарегистрируйтесь здесь <%= Html.ActionLink<AccountController>(c => c.Register(), "Регистрация")%>.
    </p>
    <%
    }
    else if (((User)User).Role.IsAdministrator)
    { %>
    <p>
        <div>Вы администратор движка.</div>
        <div>Вы можете следующее:
            <ul>
                <li>Изменить <%= Html.ActionLink<AdminAppConfigController>(c => c.Edit(), "настройки")%> сайта</li>
                <li>Управлять <%= Html.ActionLink<AdminUsersController>(c => c.Index(), "пользователями")%> (создавать, изменять свойства и удалять пользователей)</li>
                <li>Управлять <%= Html.ActionLink<AdminGamesController>(c => c.Index(), "списком игр")%> (создавать, изменять свойства и удалять игры)</li>
            </ul>
        </div>
    </p>
    <%
    } 
    else if (((User)User).Role.IsAuthor)
    { %>
    <p>
        <div>Вы автор игры <%= Html.ActionLink<GamesController>(c => c.Edit(((User)User).Game.Id), ((User)User).Game.Name)%>.</div>
        <div>Вы можете следующее:
            <ul>
                <li>Редактировать свойства своей игры</li>
                <li>Управлять состоянием игры (запускать, останавливать и пр.)</li>
                <li>Редактировать список заданий</li>
                <li>Создавать команды</li>
                <li>Регистрировать созданные команды для участия в игре</li>
            </ul>
        </div>
    </p>
    <%
    }
    else if (((User)User).Team == null)
    { %>
    <p>
        <div>Вы можете вступить в существующую команду.</div>
        <div>Если Вы станете первым игроком, вступившим в команду, то Вы автоматически станете ее капитаном.</div>
        <div>Для вступления в команду нужен ключ доступа. Капитану стедует получить его у автора игры, первым войти по нему в команду и после этого сообщить его игрокам своей команды, чтобы они тажке могли в ней зарегистрироваться.</div>
        <div>Если есть подозрения в том, что код доступа стал известен третьим лицам, следует обратиться к авторам игры с просьбой изменить код доступа.</div>
    </p>
    <%
    }
    else if (((User)User).Team != null && ((User)User).Role.IsTeamLeader)
    { %>
    <p>
        <div>Вы капитан команды <%= Html.ActionLink<TeamsController>(c => c.Show(((User)User).Team.Id), ((User)User).Team.Name)%>.</div>
        <div>Уточните, зарегистрирована ли Ваша команда на участие в интересующей Вас игре.</div>
        <div>Вы можете управлять списком игроков в Вашей команде.</div>
        <div>Вы можете сложить капитанские полномочия, в этом случае Капитаном станет игрок, стоящий в списке игроков сразу за Вами.</div>
    </p>
    <%
    } 
    else if (((User)User).Team != null && ((User)User).Role.IsPlayer)
    { %>
    <p>
        <div>Вы игрок команды <%= Html.ActionLink<TeamsController>(c => c.Show(((User)User).Team.Id), ((User)User).Team.Name)%>.</div>
        <div>Если Ваша команда зарегистрирована на участие в какой-либо игре, то ожидайте начала игры.</div>
        <div>При желании Вы можете покинуть команду, но об этом лучше поставить в известность Вашего капитана. Для повторного вступления в команду Вам потребуется код доступа.</div>
    </p>
    <%
    } %>
</asp:Content>
