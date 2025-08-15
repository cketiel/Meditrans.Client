﻿using Meditrans.Client.Commands;
using Meditrans.Client.Models;
using Meditrans.Client.Services;
using Meditrans.Client.Views.Admin.Employees; 
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Meditrans.Client.ViewModels
{
    public class UsersViewModel : BaseViewModel
    {
        #region Properties
        private ObservableCollection<User> _users;
        private User _selectedUser;
        private IUserService _userService;
        private IRoleService _roleService; 

        public ObservableCollection<User> Users
        {
            get => _users;
            set => SetProperty(ref _users, value);
        }

        public User SelectedUser
        {
            get => _selectedUser;
            set => SetProperty(ref _selectedUser, value);
        }
        #endregion

        #region Commands
        public ICommand AddUserCommand { get; }
        public ICommand EditUserCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand ChangePasswordCommand { get; }
        #endregion

        public UsersViewModel()
        {
            _userService = new UserService();
            _roleService = new RoleService(); 
            Users = new ObservableCollection<User>();

            AddUserCommand = new RelayCommandObject(AddUser);
            EditUserCommand = new RelayCommandObject(EditUser, CanEditOrDeleteUser);
            DeleteUserCommand = new RelayCommandObject(DeleteUser, CanEditOrDeleteUser);
            ChangePasswordCommand = new RelayCommandObject(OpenChangePasswordDialog, CanEditOrDeleteUser);

            LoadUsers();
        }

        private async void LoadUsers()
        {
            try
            {
                var users = await _userService.GetUsersAsync();
                Users = new ObservableCollection<User>(users);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading users: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AddUser(object parameter)
        {
            try
            {
                var availableRoles = await _roleService.GetRolesAsync();
                var addEditUserView = new AddEditUserView(null, availableRoles); 
                var result = addEditUserView.ShowDialog();

                if (result.HasValue && result.Value)
                {
                    LoadUsers();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error preparing to add user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void EditUser(object parameter)
        {
            if (SelectedUser == null) return;
            try
            {
                var availableRoles = await _roleService.GetRolesAsync();
                var addEditUserView = new AddEditUserView(SelectedUser, availableRoles);
                var result = addEditUserView.ShowDialog();

                if (result.HasValue && result.Value)
                {
                    LoadUsers();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error preparing to edit user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanEditOrDeleteUser(object parameter)
        {
            return SelectedUser != null;
        }

        private async void DeleteUser(object parameter)
        {
            if (SelectedUser == null) return;

            if (MessageBox.Show($"Are you sure you want to delete the user '{SelectedUser.FullName}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    await _userService.DeleteUserAsync(SelectedUser.Id);
                    LoadUsers();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void OpenChangePasswordDialog(object parameter)
        {
            if (SelectedUser == null) return;

            var changePasswordView = new ChangePasswordView(SelectedUser.Id);
            changePasswordView.ShowDialog();
            // There is no need to reload users here, as the password is not shown in the grid.
        }
    }
}