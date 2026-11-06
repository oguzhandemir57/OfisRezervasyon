(function () {
    'use strict';

    var app = angular.module('rayOfisApp', []);

    app.controller('MainController', ['$scope', '$http', '$timeout', function ($scope, $http, $timeout) {

        // ==================== THEME (LIGHT / DARK) ====================
        $scope.theme = localStorage.getItem('ray_ofis_theme') || 'light';
        document.body.setAttribute('data-theme', $scope.theme);

        $scope.toggleTheme = function () {
            $scope.theme = ($scope.theme === 'light') ? 'dark' : 'light';
            document.body.setAttribute('data-theme', $scope.theme);
            localStorage.setItem('ray_ofis_theme', $scope.theme);
        };

        // ==================== APP STATE ====================
        $scope.currentUser = null;
        // Tabs: 'floorplan' (Masa Seçimi) | 'checkIn' (Ofis Check-In & Rezervasyonlarım) | 'officePresence' (Ofistekiler) | 'adminDashboard'
        $scope.activeTab = 'floorplan';

        $scope.loginData = { username: '', password: '' };
        $scope.demoUsers = [];
        $scope.isLoading = false;

        $scope.daysStrip = [];
        $scope.selectedDate = '';
        $scope.selectedDateObj = new Date();

        $scope.workspaces = [];
        $scope.deskRows = [];
        $scope.meetingRooms = [];
        $scope.peopleInOffice = [];
        $scope.presenceFilter = 'CHECKED_IN'; // 'CHECKED_IN' (Doğrulanmış Ofistekiler) | 'ALL' (Gelecekler Dahil)
        $scope.summary = { total: 0, available: 0, reserved: 0, checkedIn: 0, occupancyRate: 0 };

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

        // ==================== FALLBACK STORE (GITHUB PAGES / STATIC HOSTING) ====================
        function getFallbackStore() {
            var raw = localStorage.getItem('ray_ofis_static_db_v3');
            if (raw) {
                try { return JSON.parse(raw); } catch (e) {}
            }
            var today = formatDateISO(new Date());
            var tmrDate = new Date(); tmrDate.setDate(tmrDate.getDate() + 1);
            var tomorrow = formatDateISO(tmrDate);

            var users = [
                { id: 1, username: 'admin', password: '123', fullName: 'Fatih Yayla', department: 'Yönetim', title: 'Sistem Yöneticisi', role: 'Admin', avatarInitials: 'FY' },
                { id: 2, username: 'ahmet', password: '123', fullName: 'Ahmet Yılmaz', department: 'Yazılım Geliştirme', title: 'Kıdemli .NET Geliştirici', role: 'Employee', avatarInitials: 'AY' },
                { id: 3, username: 'zeynep', password: '123', fullName: 'Zeynep Kaya', department: 'Ürün & Tasarım', title: 'UI/UX Tasarımcısı', role: 'Employee', avatarInitials: 'ZK' },
                { id: 4, username: 'murat', password: '123', fullName: 'Murat Demir', department: 'Veri Analitiği', title: 'Veri Bilimci', role: 'Employee', avatarInitials: 'MD' },
                { id: 5, username: 'elif', password: '123', fullName: 'Elif Şahin', department: 'İnsan Kaynakları', title: 'İK Uzmanı', role: 'Employee', avatarInitials: 'EŞ' },
                { id: 6, username: 'can', password: '123', fullName: 'Caner Özkan', department: 'Pazarlama', title: 'Dijital Pazarlama Uzmanı', role: 'Employee', avatarInitials: 'CÖ' }
            ];

            var workspaces = [];
            var idCounter = 1;
            for (var deskNo = 1; deskNo <= 10; deskNo++) {
                var row = Math.floor((deskNo - 1) / 2) + 1;
                var side = (deskNo % 2 === 1) ? 1 : 2;
                workspaces.push({ id: idCounter++, code: deskNo + 'A', name: 'Masa ' + deskNo + ' - Koltuk ' + deskNo + 'A', type: 'Masa', zone: 'Masa ' + deskNo, capacity: 1, gridX: side, gridY: row, gridW: deskNo, gridH: 1, isActive: true });
                workspaces.push({ id: idCounter++, code: deskNo + 'B', name: 'Masa ' + deskNo + ' - Koltuk ' + deskNo + 'B', type: 'Masa', zone: 'Masa ' + deskNo, capacity: 1, gridX: side, gridY: row, gridW: deskNo, gridH: 2, isActive: true });
            }
            workspaces.push({ id: idCounter++, code: 'ODA-1', name: 'Toplantı Odası 1', type: 'Oda', zone: 'Toplantı Odaları', capacity: 6, isActive: true });
            workspaces.push({ id: idCounter++, code: 'ODA-2', name: 'Toplantı Odası 2', type: 'Oda', zone: 'Toplantı Odaları', capacity: 8, isActive: true });
            workspaces.push({ id: idCounter++, code: 'ODA-3', name: 'Toplantı Odası 3', type: 'Oda', zone: 'Toplantı Odaları', capacity: 4, isActive: true });
            workspaces.push({ id: idCounter++, code: 'ODA-4', name: 'Büyük Konferans Odası', type: 'Oda', zone: 'Toplantı Odaları', capacity: 12, isActive: true });

            var reservations = [
                { id: 1, workspaceItemId: 1, userId: 2, date: today, timeSlot: 'Tam Gün (09:00 - 18:00)', note: 'Sprint geliştirme', status: 'CheckedIn' },
                { id: 2, workspaceItemId: 4, userId: 3, date: today, timeSlot: 'Tam Gün (09:00 - 18:00)', note: 'UI/UX Tasarım', status: 'CheckedIn' },
                { id: 3, workspaceItemId: 5, userId: 4, date: today, timeSlot: 'Tam Gün (09:00 - 18:00)', note: 'Veri Raporlama', status: 'CheckedIn' },
                { id: 4, workspaceItemId: 6, userId: 5, date: today, timeSlot: 'Tam Gün (09:00 - 18:00)', note: 'Mülakatlar', status: 'Active' },
                { id: 5, workspaceItemId: 11, userId: 6, date: today, timeSlot: 'Tam Gün (09:00 - 18:00)', note: 'Kampanya Planı', status: 'CheckedIn' },
                { id: 6, workspaceItemId: 21, userId: 1, date: today, timeSlot: 'Tam Gün (09:00 - 18:00)', note: 'Yönetim Toplantısı', status: 'CheckedIn' },
                { id: 7, workspaceItemId: 2, userId: 2, date: tomorrow, timeSlot: 'Tam Gün (09:00 - 18:00)', note: 'Ofis çalışması', status: 'Active' }
            ];

            var db = { users: users, workspaces: workspaces, reservations: reservations };
            localStorage.setItem('ray_ofis_static_db_v3', JSON.stringify(db));
            return db;
        }

        function saveFallbackStore(db) {
            localStorage.setItem('ray_ofis_static_db_v3', JSON.stringify(db));
        }

        $scope.showToast = function (message, isError) {
            var toast = { id: Date.now() + Math.random(), message: message, isError: !!isError };
            $scope.toasts.push(toast);
            $timeout(function () {
                var idx = $scope.toasts.indexOf(toast);
                if (idx !== -1) $scope.toasts.splice(idx, 1);
            }, 3800);
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

        function buildBusLayoutAndPresence(items) {
            var desksMap = {};
            var rooms = [];
            var presenceList = [];

            items.forEach(function (item) {
                if (item.isReserved && item.reservation) {
                    presenceList.push({
                        workspaceCode: item.code,
                        workspaceName: item.name,
                        workspaceType: item.type,
                        userId: item.reservation.userId,
                        fullName: item.reservation.userFullName,
                        department: item.reservation.userDepartment,
                        initials: item.reservation.userInitials || item.reservation.userFullName.substring(0, 2).toUpperCase(),
                        timeSlot: item.reservation.timeSlot,
                        status: item.reservation.status // 'CheckedIn' or 'Active'
                    });
                }

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
            $scope.peopleInOffice = presenceList;
        }

        $scope.getFilteredPresence = function () {
            return $scope.peopleInOffice.filter(function (p) {
                if ($scope.presenceFilter === 'CHECKED_IN') {
                    return p.status === 'CheckedIn';
                }
                return true;
            });
        };

        $scope.getCheckedInCount = function () {
            return $scope.peopleInOffice.filter(function (p) { return p.status === 'CheckedIn'; }).length;
        };

        $scope.getPendingCheckInCount = function () {
            return $scope.myReservations.filter(function (r) { return r.status !== 'CheckedIn'; }).length;
        };

        function init() {
            buildDaysStrip();
            $http.get('/api/users').then(function (res) {
                $scope.demoUsers = res.data;
            }).catch(function () {
                $scope.demoUsers = getFallbackStore().users;
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
                        onLoginSuccess(res.data.user, res.data.message);
                    }
                })
                .catch(function (err) {
                    $scope.isLoading = false;
                    if (err.status === 401 && err.data && err.data.message) {
                        $scope.showToast(err.data.message, true);
                        return;
                    }
                    var db = getFallbackStore();
                    var u = db.users.find(function (x) {
                        return x.username.toLowerCase() === $scope.loginData.username.trim().toLowerCase() && x.password === $scope.loginData.password;
                    });
                    if (u) {
                        onLoginSuccess(u, 'Hoş geldiniz, ' + u.fullName + '!');
                    } else {
                        $scope.showToast('Kullanıcı adı veya şifre hatalı!', true);
                    }
                });
        };

        function onLoginSuccess(user, msg) {
            $scope.currentUser = user;
            localStorage.setItem('ray_ofis_user', JSON.stringify($scope.currentUser));
            $scope.activeTab = 'floorplan';
            $scope.showToast(msg, false);
            loadWorkspaces();
            loadMyReservations();
            if ($scope.currentUser.role === 'Admin') {
                loadAdminDashboard();
            }
        }

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
            loadWorkspaces();
            loadMyReservations();
            if ($scope.currentUser && $scope.currentUser.role === 'Admin') {
                loadAdminDashboard();
            }
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

        function applyWorkspacesData(items, summary) {
            $scope.workspaces = items || [];
            $scope.summary = summary || {};
            buildBusLayoutAndPresence($scope.workspaces);

            if ($scope.selectedItem) {
                var updated = $scope.workspaces.find(function (w) { return w.id === $scope.selectedItem.id; });
                $scope.selectedItem = updated || null;
            } else if ($scope.workspaces.length > 0) {
                var firstFree = $scope.workspaces.find(function (w) { return w.isActive && !w.isReserved; });
                $scope.selectedItem = firstFree || $scope.workspaces[0];
            }
        }

        function loadWorkspaces() {
            if (!$scope.currentUser) return;
            $http.get('/api/workspaces?date=' + encodeURIComponent($scope.selectedDate))
                .then(function (res) {
                    applyWorkspacesData(res.data.items, res.data.summary);
                })
                .catch(function () {
                    var db = getFallbackStore();
                    var dayRes = db.reservations.filter(function (r) { return r.date === $scope.selectedDate && r.status !== 'Cancelled'; });
                    var items = db.workspaces.map(function (w) {
                        var res = dayRes.find(function (r) { return r.workspaceItemId === w.id; });
                        var u = res ? db.users.find(function (x) { return x.id === res.userId; }) : null;
                        return angular.extend({}, w, {
                            isReserved: !!res,
                            reservation: res ? {
                                id: res.id,
                                userId: res.userId,
                                userFullName: u ? u.fullName : 'Dolu',
                                userDepartment: u ? u.department : '',
                                userInitials: u ? u.avatarInitials : 'D',
                                date: res.date,
                                timeSlot: res.timeSlot,
                                note: res.note,
                                status: res.status
                            } : null
                        });
                    });
                    var reserved = items.filter(function (i) { return i.isReserved; }).length;
                    var avail = items.length - reserved;
                    var checkedIn = dayRes.filter(function (r) { return r.status === 'CheckedIn'; }).length;
                    applyWorkspacesData(items, {
                        total: items.length,
                        available: avail,
                        reserved: reserved,
                        checkedIn: checkedIn,
                        occupancyRate: Math.round((reserved / items.length) * 100)
                    });
                });
        }

        $scope.isMyReservation = function (item) {
            return item && item.isReserved && item.reservation && $scope.currentUser && item.reservation.userId === $scope.currentUser.id;
        };

        $scope.getBusSeatClass = function (item) {
            if (!item) return '';
            var cls = [];
            if ($scope.selectedItem && $scope.selectedItem.id === item.id) {
                cls.push('seat-selected');
            }
            if (item.isReserved) {
                if ($scope.isMyReservation(item)) {
                    cls.push('seat-mine');
                } else {
                    cls.push('seat-reserved');
                }
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
                    if ($scope.currentUser.role === 'Admin') loadAdminDashboard();
                })
                .catch(function (err) {
                    if (err.status === 400 && err.data && err.data.message) {
                        $scope.showToast(err.data.message, true);
                        return;
                    }
                    var db = getFallbackStore();
                    var nextId = db.reservations.length > 0 ? Math.max.apply(null, db.reservations.map(function (r) { return r.id; })) + 1 : 1;
                    db.reservations.push({
                        id: nextId,
                        workspaceItemId: payload.workspaceItemId,
                        userId: payload.userId,
                        date: payload.date,
                        timeSlot: payload.timeSlot,
                        note: payload.note || 'Uzaktan / Hibrit Ofis Çalışması',
                        status: 'Active'
                    });
                    saveFallbackStore(db);
                    $scope.showToast($scope.selectedItem.name + ' ayrıldı! Ofise gittiğinizde Check-In sekmesinden doğrulayabilirsiniz.', false);
                    loadWorkspaces();
                    loadMyReservations();
                    if ($scope.currentUser.role === 'Admin') loadAdminDashboard();
                });
        };

        $scope.checkInReservation = function (reservationId) {
            if (!$scope.currentUser) return;
            $http.post('/api/reservations/' + reservationId + '/checkin?userId=' + $scope.currentUser.id, {})
                .then(function (res) {
                    $scope.showToast(res.data.message, false);
                    loadWorkspaces();
                    loadMyReservations();
                    if ($scope.currentUser.role === 'Admin') loadAdminDashboard();
                })
                .catch(function () {
                    var db = getFallbackStore();
                    var r = db.reservations.find(function (x) { return x.id === reservationId; });
                    if (r) r.status = 'CheckedIn';
                    saveFallbackStore(db);
                    $scope.showToast('Ofis Check-In işleminiz doğrulandı! Artık Ofistekiler sayfasında görünüyorsunuz.', false);
                    loadWorkspaces();
                    loadMyReservations();
                    if ($scope.currentUser.role === 'Admin') loadAdminDashboard();
                });
        };

        $scope.cancelReservation = function (reservationId) {
            if (!$scope.currentUser) return;
            $http.delete('/api/reservations/' + reservationId + '?userId=' + $scope.currentUser.id)
                .then(function (res) {
                    $scope.showToast(res.data.message, false);
                    loadWorkspaces();
                    loadMyReservations();
                    if ($scope.currentUser.role === 'Admin') loadAdminDashboard();
                })
                .catch(function () {
                    var db = getFallbackStore();
                    var r = db.reservations.find(function (x) { return x.id === reservationId; });
                    if (r) r.status = 'Cancelled';
                    saveFallbackStore(db);
                    $scope.showToast('Rezervasyon iptal edildi.', false);
                    loadWorkspaces();
                    loadMyReservations();
                    if ($scope.currentUser.role === 'Admin') loadAdminDashboard();
                });
        };

        function loadMyReservations() {
            if (!$scope.currentUser) return;
            $http.get('/api/my-reservations?userId=' + $scope.currentUser.id)
                .then(function (res) {
                    $scope.myReservations = res.data || [];
                })
                .catch(function () {
                    var db = getFallbackStore();
                    $scope.myReservations = db.reservations
                        .filter(function (r) { return r.userId === $scope.currentUser.id && r.status !== 'Cancelled'; })
                        .map(function (r) {
                            var ws = db.workspaces.find(function (w) { return w.id === r.workspaceItemId; });
                            return {
                                id: r.id,
                                workspaceCode: ws ? ws.code : '',
                                workspaceName: ws ? ws.name : '',
                                workspaceZone: ws ? ws.zone : '',
                                date: r.date,
                                timeSlot: r.timeSlot,
                                note: r.note,
                                status: r.status
                            };
                        });
                });
        }

        function loadAdminDashboard() {
            $http.get('/api/admin/dashboard?date=' + encodeURIComponent($scope.selectedDate))
                .then(function (res) {
                    $scope.adminData = res.data;
                })
                .catch(function () {
                    var db = getFallbackStore();
                    var dayRes = db.reservations.filter(function (r) { return r.date === $scope.selectedDate && r.status !== 'Cancelled'; });
                    var allDetailed = db.reservations.slice().reverse().map(function (r) {
                        var u = db.users.find(function (x) { return x.id === r.userId; });
                        var w = db.workspaces.find(function (x) { return x.id === r.workspaceItemId; });
                        return {
                            id: r.id,
                            date: r.date,
                            timeSlot: r.timeSlot,
                            note: r.note,
                            status: r.status,
                            userFullName: u ? u.fullName : 'Bilinmeyen',
                            userDepartment: u ? u.department : '-',
                            workspaceCode: w ? w.code : '-',
                            workspaceName: w ? w.name : '-'
                        };
                    });
                    $scope.adminData = {
                        kpis: {
                            occupancyRate: Math.round((dayRes.length / db.workspaces.length) * 100),
                            reservedDesks: dayRes.filter(function (r) { return r.workspaceItemId <= 20; }).length,
                            totalDesks: 20,
                            reservedRooms: dayRes.filter(function (r) { return r.workspaceItemId > 20; }).length,
                            totalRooms: 4,
                            checkedInCount: dayRes.filter(function (r) { return r.status === 'CheckedIn'; }).length,
                            availableToday: db.workspaces.length - dayRes.length
                        },
                        allReservations: allDetailed
                    };
                });
        }

        init();
    }]);
})();
