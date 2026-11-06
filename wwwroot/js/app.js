(function () {
    'use strict';

    var app = angular.module('rayOfisApp', []);

    app.controller('MainController', ['$scope', '$http', '$timeout', function ($scope, $http, $timeout) {

        $scope.currentUser = null;
        $scope.activeTab = 'floorplan'; // 'floorplan' | 'myReservations' | 'adminDashboard'

        $scope.loginData = { username: '', password: '' };
        $scope.demoUsers = [];
        $scope.isLoading = false;

        $scope.daysStrip = [];
        $scope.selectedDate = '';
        $scope.selectedDateObj = new Date();

        $scope.workspaces = [];
        $scope.deskRows = []; // 5 rows, each with leftDesk and rightDesk (each desk has 2 seats)
        $scope.meetingRooms = [];
        $scope.summary = { total: 0, available: 0, reserved: 0, occupancyRate: 0 };

        $scope.selectedItem = null;
        $scope.bookingForm = {
            timeSlot: 'Tam Gün (09:00 - 18:00)',
            note: ''
        };

        $scope.timeSlots = [
            'Tam Gün (09:00 - 18:00)',
            'Öğleden Önce (09:00 - 13:00)',
            'Öğleden Sonra (13:30 - 18:00)'
        ];

        $scope.myReservations = [];
        $scope.adminData = null;
        $scope.adminSearch = '';
        $scope.toasts = [];

        $scope.showToast = function (message, isError) {
            var toast = { id: Date.now() + Math.random(), message: message, isError: !!isError };
            $scope.toasts.push(toast);
            $timeout(function () {
                var idx = $scope.toasts.indexOf(toast);
                if (idx !== -1) $scope.toasts.splice(idx, 1);
            }, 3500);
        };

        function formatDateISO(d) {
            var year = d.getFullYear();
            var month = ('0' + (d.getMonth() + 1)).slice(-2);
            var day = ('0' + d.getDate()).slice(-2);
            return year + '-' + month + '-' + day;
        }

        function buildDaysStrip() {
            var dayNames = ['Paz', 'Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt'];
            var monthNames = ['Oca', 'Şub', 'Mar', 'Nis', 'May', 'Haz', 'Tem', 'Ağu', 'Eyl', 'Eki', 'Kas', 'Ara'];
            var list = [];
            var today = new Date();
            for (var i = 0; i < 7; i++) {
                var d = new Date(today);
                d.setDate(today.getDate() + i);
                var iso = formatDateISO(d);
                list.push({
                    iso: iso,
                    dayName: i === 0 ? 'Bugün' : (i === 1 ? 'Yarın' : dayNames[d.getDay()]),
                    dayNum: d.getDate(),
                    monthName: monthNames[d.getMonth()],
                    fullLabel: d.getDate() + ' ' + monthNames[d.getMonth()] + ' ' + d.getFullYear()
                });
            }
            $scope.daysStrip = list;
            $scope.selectedDate = list[0].iso;
            $scope.selectedDateObj = new Date(today.getFullYear(), today.getMonth(), today.getDate());
        }

        function buildBusLayout(items) {
            var desksMap = {};
            var rooms = [];

            items.forEach(function (item) {
                if (item.type === 'Oda') {
                    rooms.push(item);
                } else {
                    var deskNo = item.gridW || 1;
                    if (!desksMap[deskNo]) {
                        desksMap[deskNo] = {
                            deskNo: deskNo,
                            title: 'MASA ' + deskNo + ' (2 Kişilik)',
                            seats: []
                        };
                    }
                    desksMap[deskNo].seats.push(item);
                }
            });

            var rows = [];
            for (var r = 0; r < 5; r++) {
                var leftDeskNo = r * 2 + 1;
                var rightDeskNo = r * 2 + 2;
                rows.push({
                    rowIndex: r + 1,
                    leftDesk: desksMap[leftDeskNo] || { deskNo: leftDeskNo, title: 'MASA ' + leftDeskNo, seats: [] },
                    rightDesk: desksMap[rightDeskNo] || { deskNo: rightDeskNo, title: 'MASA ' + rightDeskNo, seats: [] }
                });
            }

            $scope.deskRows = rows;
            $scope.meetingRooms = rooms;
        }

        function init() {
            buildDaysStrip();
            $http.get('/api/users').then(function (res) {
                $scope.demoUsers = res.data;
            });

            var savedUser = localStorage.getItem('ray_ofis_user');
            if (savedUser) {
                try {
                    $scope.currentUser = JSON.parse(savedUser);
                    loadWorkspaces();
                    loadMyReservations();
                    if ($scope.currentUser.role === 'Admin') {
                        loadAdminDashboard();
                    }
                } catch (e) {
                    localStorage.removeItem('ray_ofis_user');
                }
            }
        }

        $scope.login = function () {
            if (!$scope.loginData.username || !$scope.loginData.password) {
                $scope.showToast('Kullanıcı adı ve şifre giriniz.', true);
                return;
            }
            $scope.isLoading = true;
            $http.post('/api/auth/login', $scope.loginData)
                .then(function (res) {
                    $scope.isLoading = false;
                    if (res.data && res.data.success) {
                        $scope.currentUser = res.data.user;
                        localStorage.setItem('ray_ofis_user', JSON.stringify($scope.currentUser));
                        $scope.activeTab = 'floorplan';
                        $scope.showToast(res.data.message, false);
                        loadWorkspaces();
                        loadMyReservations();
                        if ($scope.currentUser.role === 'Admin') {
                            loadAdminDashboard();
                        }
                    }
                })
                .catch(function (err) {
                    $scope.isLoading = false;
                    var msg = (err.data && err.data.message) ? err.data.message : 'Giriş yapılamadı.';
                    $scope.showToast(msg, true);
                });
        };

        $scope.quickLogin = function (username) {
            $scope.loginData.username = username;
            $scope.loginData.password = '123';
            $scope.login();
        };

        $scope.logout = function () {
            localStorage.removeItem('ray_ofis_user');
            $scope.currentUser = null;
            $scope.selectedItem = null;
            $scope.loginData = { username: '', password: '' };
        };

        $scope.switchTab = function (tabName) {
            $scope.activeTab = tabName;
            if (tabName === 'floorplan') loadWorkspaces();
            if (tabName === 'myReservations') loadMyReservations();
            if (tabName === 'adminDashboard') loadAdminDashboard();
        };

        $scope.selectDate = function (isoDate) {
            $scope.selectedDate = isoDate;
            var parts = isoDate.split('-');
            if (parts.length === 3) {
                $scope.selectedDateObj = new Date(parseInt(parts[0]), parseInt(parts[1]) - 1, parseInt(parts[2]));
            }
            loadWorkspaces();
            if ($scope.currentUser && $scope.currentUser.role === 'Admin') {
                loadAdminDashboard();
            }
        };

        $scope.onDateInputChange = function () {
            if ($scope.selectedDateObj instanceof Date && !isNaN($scope.selectedDateObj)) {
                $scope.selectedDate = formatDateISO($scope.selectedDateObj);
                loadWorkspaces();
                if ($scope.currentUser && $scope.currentUser.role === 'Admin') {
                    loadAdminDashboard();
                }
            }
        };

        function loadWorkspaces() {
            if (!$scope.currentUser) return;
            $http.get('/api/workspaces?date=' + encodeURIComponent($scope.selectedDate))
                .then(function (res) {
                    $scope.workspaces = res.data.items || [];
                    $scope.summary = res.data.summary || {};
                    buildBusLayout($scope.workspaces);

                    if ($scope.selectedItem) {
                        var updated = $scope.workspaces.find(function (w) { return w.id === $scope.selectedItem.id; });
                        $scope.selectedItem = updated || null;
                    } else if ($scope.workspaces.length > 0) {
                        var firstFree = $scope.workspaces.find(function (w) { return w.isActive && !w.isReserved; });
                        $scope.selectedItem = firstFree || $scope.workspaces[0];
                    }
                });
        }

        $scope.getBusSeatClass = function (item) {
            if (!item) return '';
            var cls = [];
            if ($scope.selectedItem && $scope.selectedItem.id === item.id) {
                cls.push('seat-selected');
            }
            if (item.isReserved) {
                cls.push('seat-reserved');
            } else {
                cls.push('seat-empty');
            }
            return cls.join(' ');
        };

        $scope.selectWorkspace = function (item) {
            $scope.selectedItem = item;
            $scope.bookingForm.note = '';
        };

        $scope.createReservation = function () {
            if (!$scope.selectedItem || !$scope.currentUser) return;

            var payload = {
                workspaceItemId: $scope.selectedItem.id,
                userId: $scope.currentUser.id,
                date: $scope.selectedDate,
                timeSlot: $scope.bookingForm.timeSlot,
                note: $scope.bookingForm.note
            };

            $http.post('/api/reservations', payload)
                .then(function (res) {
                    $scope.showToast(res.data.message, false);
                    $scope.bookingForm.note = '';
                    loadWorkspaces();
                    loadMyReservations();
                    if ($scope.currentUser.role === 'Admin') {
                        loadAdminDashboard();
                    }
                })
                .catch(function (err) {
                    var msg = (err.data && err.data.message) ? err.data.message : 'Rezervasyon yapılamadı.';
                    $scope.showToast(msg, true);
                });
        };

        $scope.cancelReservation = function (reservationId) {
            if (!$scope.currentUser) return;
            $http.delete('/api/reservations/' + reservationId + '?userId=' + $scope.currentUser.id)
                .then(function (res) {
                    $scope.showToast(res.data.message, false);
                    loadWorkspaces();
                    loadMyReservations();
                    if ($scope.currentUser.role === 'Admin') {
                        loadAdminDashboard();
                    }
                })
                .catch(function (err) {
                    var msg = (err.data && err.data.message) ? err.data.message : 'İptal edilemedi.';
                    $scope.showToast(msg, true);
                });
        };

        function loadMyReservations() {
            if (!$scope.currentUser) return;
            $http.get('/api/my-reservations?userId=' + $scope.currentUser.id)
                .then(function (res) {
                    $scope.myReservations = res.data || [];
                });
        }

        function loadAdminDashboard() {
            $http.get('/api/admin/dashboard?date=' + encodeURIComponent($scope.selectedDate))
                .then(function (res) {
                    $scope.adminData = res.data;
                });
        }

        init();
    }]);
})();
