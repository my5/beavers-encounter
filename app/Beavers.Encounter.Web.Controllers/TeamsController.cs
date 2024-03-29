using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Microsoft.Practices.ServiceLocation;

using SharpArch.Core;
using SharpArch.Core.PersistenceSupport;
using SharpArch.Web.NHibernate;

using Beavers.Encounter.Common;
using Beavers.Encounter.Common.Filters;
using Beavers.Encounter.Common.MvcContrib;
using Beavers.Encounter.Core;
using Beavers.Encounter.Core.DataInterfaces;
using Beavers.Encounter.Web.Controllers.Filters;
using Beavers.Encounter.Web.Controllers.Binders;

namespace Beavers.Encounter.Web.Controllers
{
    [Authorize]
    [LockIfGameStart]
    [HandleError]
    public class TeamsController : BaseController
    {
        private readonly IRepository<Team> teamRepository;
        private readonly IRepository<Game> gameRepository;

        public TeamsController(IRepository<Team> teamRepository, IUserRepository userRepository, IRepository<Game> gameRepository)
            : base(userRepository)
        {
            Check.Require(teamRepository != null, "teamRepository may not be null");
            Check.Require(gameRepository != null, "gameRepository may not be null");

            this.teamRepository = teamRepository;
            this.gameRepository = gameRepository;
        }

        [Breadcrumb("������ ������", 2)]
        [GameState]
        public ActionResult Index() {
            IList<Team> teams = teamRepository.GetAll();

            return View(teams);
        }

        [Breadcrumb("������� \"{0}\"", 3)]
        [GameState]
        public ActionResult Show(int id) {
            Team team = teamRepository.Get(id);
            this.SetBreadcrumbText(team.Name);
            return View(team);
        }

        [Breadcrumb("����� �������", 3)]
        [AuthorsOnly]
        [GameState]
        public ActionResult Create()
        {
            TeamFormViewModel viewModel = TeamFormViewModel.CreateTeamFormViewModel();
            return View(viewModel);
        }

        [AuthorsOnly]
        [GameState]
        [ValidateAntiForgeryToken]
        [Transaction]
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Create([TeamBinder(Fetch = false)]Team team)
        {
            if (ViewData.ModelState.IsValid && team.IsValid()) 
            {
                // ������ ����� ��������� �������, �� �� ����� �������� ������� ��������� �������
                if (!User.Role.IsAuthor)
                {
                    // ������ �������� ������������ ��������� ��������� �������
                    team.TeamLeader = User;

                    // ������ �������� ������������ ������ ��������� �������
                    User.Team = team;
                }

                teamRepository.SaveOrUpdate(team);

                Message = "������� ������� �������.";
                return this.RedirectToAction(c => c.Index());
            }

            TeamFormViewModel viewModel = TeamFormViewModel.CreateTeamFormViewModel();
            viewModel.Team = team;
            return View(viewModel);
        }

        [Breadcrumb("������� \"{0}\"", 3)]
        [AuthorsOnly]
        [GameState]
        public ActionResult Edit(int id) {
            TeamFormViewModel viewModel = TeamFormViewModel.CreateTeamFormViewModel();
            viewModel.Team = teamRepository.Get(id);
            this.SetBreadcrumbText(viewModel.Team.Name);
            return View(viewModel);
        }

        [AuthorsOnly]
        [GameState]
        [ValidateAntiForgeryToken]
        [Transaction]
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Edit([TeamBinder(Fetch = true)]Team team)
        {
            Team teamToUpdate = teamRepository.Get(team.Id);
            TransferFormValuesTo(teamToUpdate, team);

            if (ViewData.ModelState.IsValid && team.IsValid()) {
                Message = "������� ������� ��������.";
                return this.RedirectToAction(c => c.Index());
            }
            
            teamRepository.DbContext.RollbackTransaction();

			TeamFormViewModel viewModel = TeamFormViewModel.CreateTeamFormViewModel();
			viewModel.Team = team;
			return View(viewModel);
        }

        private void TransferFormValuesTo(Team teamToUpdate, Team teamFromForm) {
			teamToUpdate.Name = teamFromForm.Name;
            teamToUpdate.AccessKey = teamFromForm.AccessKey;
            teamToUpdate.FinalTask = teamFromForm.FinalTask;
        }

        [AuthorsOnly]
        [GameState]
        [ValidateAntiForgeryToken]
        [Transaction]
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Delete(int id) {
            string resultMessage = "������� ������� �������.";
            Team teamToDelete = teamRepository.Get(id);

            if (teamToDelete != null) {
                User.Team = null;
                teamRepository.Delete(teamToDelete);

                try {
                    teamRepository.DbContext.CommitChanges();
                }
                catch {
                    resultMessage = "�� ���� ������ ������� �� ����� ���� �������. ��������, ��� ����� ���-�� ���.";
                    teamRepository.DbContext.RollbackTransaction();
                }
            }
            else {
                resultMessage = "��������� ������� �� �������. ��������, �� ��� �������.";
            }

            Message = resultMessage;
            return this.RedirectToAction(c => c.Index());
        }

        [GameState]
        public ActionResult SingIn(int id)
        {
            TeamFormViewModel1 model = TeamFormViewModel1.CreateTeamFormViewModel1();

            model.TeamId = id;

            return View(model);
        }

        [GameState]
        [ValidateAntiForgeryToken]
        [Transaction]
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SingIn(int id, string accessKey)
        {
            Team team = teamRepository.Get(id);

            if (team.AccessKey == accessKey)
            {
                IUserRepository userRepository = ServiceLocator.Current.GetInstance<IUserRepository>();

                User user = userRepository.GetByLogin(HttpContext.User.Identity.Name);
                user.Team = team;

                // ������� ������ ��������� � ������� ������ ��������� �������
                if (team.TeamLeader == null)
                    team.TeamLeader = user;

                return this.RedirectToAction(c => c.Show(id));
            }
            Message = "�������� ��� �������! �������� ��� � ������ ��������.";
            return this.RedirectToAction(c => c.SingIn(id));
        }

        /// <summary>
        /// �������� �������.
        /// </summary>
        /// <returns></returns>
        [GameState]
        [Transaction]
        public ActionResult SingOut()
        {
            IUserRepository userRepository = ServiceLocator.Current.GetInstance<IUserRepository>();
            
            User user = userRepository.GetByLogin(HttpContext.User.Identity.Name);

            Team team = user.Team;

            // ���� ����� ��� ���������, �� ��������� ��� �����������
            if (team.TeamLeader == user)
                team.TeamLeader = null;

            team.Users.Remove(user);
            user.Team = null;

            // ���� ������� �������� ��� ��������, 
            // �� ������ ������� ������������ �� ������ ������ ������� ���������
            if (team.TeamLeader == null && team.Users.Count > 0)
                team.TeamLeader = team.Users.First();

            return this.RedirectToAction(c => c.Show(team.Id));
        }

        /// <summary>
        /// ������� ������ �� �������.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        [TeamLeadersOnly]
        [GameState]
        [Transaction]
        public ActionResult SingOutUser(int id, int userId)
        {
            IUserRepository userRepository = ServiceLocator.Current.GetInstance<IUserRepository>();

            User user = userRepository.Get(userId);

            Team team = user.Team;

            if (User.Team.Id != team.Id)
            {
                Message = "������������ ��������!";
                return this.RedirectToAction(c => c.Show(User.Team.Id));
            }

            User.Team.Users.Remove(user);
            user.Team = null;

            return this.RedirectToAction(c => c.Show(User.Team.Id));
        }

        [AuthorsOnly]
        [GameState]
        [Transaction]
        public ActionResult SingInGame(int gameId, int teamId)
        {
            Game game = gameRepository.Get(gameId);
            Team team = teamRepository.Get(teamId);
            game.Teams.Add(team);

            return this.RedirectToAction(c => c.Index());
        }

        [AuthorsOnly]
        [GameState]
        [Transaction]
        public ActionResult SingOutGame(int gameId, int teamId)
        {
            Game game = gameRepository.Get(gameId);
            Team team = teamRepository.Get(teamId);
            game.Teams.Remove(team);

            return this.RedirectToAction<GamesController>(c => c.Edit(gameId));
        }

        /// <summary>
		/// Holds data to be passed to the Team form for creates and edits
		/// </summary>
        public class TeamFormViewModel
        {
            private TeamFormViewModel() { }

			/// <summary>
			/// Creation method for creating the view model. Services may be passed to the creation 
			/// method to instantiate items such as lists for drop down boxes.
			/// </summary>
            public static TeamFormViewModel CreateTeamFormViewModel() {
                TeamFormViewModel viewModel = new TeamFormViewModel();
                
                return viewModel;
            }

            public Team Team { get; internal set; }
        }

        /// <summary>
        /// Holds data to be passed to the Team form for creates and edits
        /// </summary>
        public class TeamFormViewModel1
        {
            private TeamFormViewModel1() { }

            /// <summary>
            /// Creation method for creating the view model. Services may be passed to the creation 
            /// method to instantiate items such as lists for drop down boxes.
            /// </summary>
            public static TeamFormViewModel1 CreateTeamFormViewModel1()
            {
                TeamFormViewModel1 viewModel = new TeamFormViewModel1();

                return viewModel;
            }

            public int TeamId { get; internal set; }
        }
    }
}
