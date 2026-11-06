(function () {
    'use strict';

    var app = angular.module('rayOfisApp', []);

    app.controller('MainController', ['$scope', '$http', '$timeout', '$sce', function ($scope, $http, $timeout, $sce) {

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
        $scope.activeTab = 'floorplan';

        $scope.loginData = { username: '', password: '' };
        $scope.demoUsers = [];
        $scope.isLoading = false;

        $scope.daysStrip = [];
        $scope.todayDate = formatDateISO(new Date());
        $scope.selectedDate = $scope.todayDate;
        $scope.selectedDateObj = new Date();

        $scope.workspaces = [];
        $scope.deskRows = [];
        $scope.meetingRooms = [];
        $scope.peopleInOffice = [];
        $scope.presenceFilter = 'CHECKED_IN';
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

        $scope.isToday = function (dateStr) {
            return dateStr === $scope.todayDate;
        };

        // ==================== POP-UP MODALS ====================
        $scope.resultPopup = {
            visible: false,
            title: '',
            message: '',
            reservedItem: null
        };

        $scope.changeConfirmPopup = {
            visible: false,
            existingCode: '',
            newWorkspaceId: 0,
            newCode: '',
            newName: '',
            itemType: 'Masa',
            date: '',
            timeSlot: '',
            note: '',
            message: ''
        };

        $scope.openResultPopup = function (title, message, reservedItem) {
            $scope.resultPopup = {
                visible: true,
                title: title || 'İşlem Tamamlandı!',
                message: message || '',
                reservedItem: reservedItem || null
            };
        };

        $scope.closeResultPopup = function () {
            $scope.resultPopup.visible = false;
        };

        $scope.closeChangeConfirmPopup = function () {
            $scope.changeConfirmPopup.visible = false;
        };

        $scope.confirmReplaceDesk = function () {
            var info = $scope.changeConfirmPopup;
            $scope.changeConfirmPopup.visible = false;
            executeCreateReservation({
                workspaceItemId: info.newWorkspaceId,
                userId: $scope.currentUser.id,
                date: info.date,
                timeSlot: info.timeSlot,
                note: info.note,
                replaceExisting: true
            });
        };

        // ==================== GOOGLE & OUTLOOK CALENDAR LINKS ====================
        function getSlotStartEnd(dateStr, timeSlot) {
            var cleanDate = (dateStr || formatDateISO(new Date())).replace(/-/g, '');
            var startH = '090000', endH = '180000';
            var isoStart = '09:00:00', isoEnd = '18:00:00';
            if (timeSlot && timeSlot.indexOf('Öğleden Önce') !== -1) {
                endH = '130000'; isoEnd = '13:00:00';
            } else if (timeSlot && timeSlot.indexOf('Öğleden Sonra') !== -1) {
                startH = '133000'; isoStart = '13:30:00';
            }
            return {
                googleDates: cleanDate + 'T' + startH + '/' + cleanDate + 'T' + endH,
                outlookStart: dateStr + 'T' + isoStart,
                outlookEnd: dateStr + 'T' + isoEnd
            };
        }

        $scope.addToGoogleCalendar = function (item) {
            if (!item) return;
            var code = item.code || item.workspaceCode;
            var name = item.name || item.workspaceName;
            var date = item.date;
            var slot = item.timeSlot || 'Tam Gün (09:00 - 18:00)';
            var dt = getSlotStartEnd(date, slot);

            var title = encodeURIComponent('RAY OFİS Masa Rezervasyonu: ' + code + ' (' + name + ')');
            var details = encodeURIComponent('RAY Ofis Rezervasyon Sistemi üzerinden ayrıldı.\nMasa/Oda: ' + code + ' - ' + name + '\nSaat: ' + slot + '\nOfise geldiğinizde aynı gün Check-In yapmayı unutmayın.');
            var location = encodeURIComponent('RAY Merkez Ofis - ' + code);

            var url = 'https://calendar.google.com/calendar/render?action=TEMPLATE&text=' + title + '&dates=' + dt.googleDates + '&details=' + details + '&location=' + location;
            window.open(url, '_blank');
        };

        $scope.addToOutlookCalendar = function (item) {
            if (!item) return;
            var code = item.code || item.workspaceCode;
            var name = item.name || item.workspaceName;
            var date = item.date;
            var slot = item.timeSlot || 'Tam Gün (09:00 - 18:00)';
            var dt = getSlotStartEnd(date, slot);

            var subject = encodeURIComponent('RAY OFİS Masa Rezervasyonu: ' + code + ' (' + name + ')');
            var body = encodeURIComponent('RAY Ofis Rezervasyon Sistemi üzerinden ayrıldı.\nMasa/Oda: ' + code + ' - ' + name + '\nSaat: ' + slot + '\nOfise geldiğinizde aynı gün Check-In yapmayı unutmayın.');
            var location = encodeURIComponent('RAY Merkez Ofis - ' + code);

            var outlookUrl = 'https://outlook.office.com/calendar/0/deeplink/compose?path=/calendar/action/compose&rru=addevent&subject=' + subject + '&startdt=' + encodeURIComponent(dt.outlookStart) + '&enddt=' + encodeURIComponent(dt.outlookEnd) + '&body=' + body + '&location=' + location;
            window.open(outlookUrl, '_blank');
        };

        // ==================== RAY AI CHATBOT STATE (GEMINI FREE AI) ====================
        $scope.aiState = {
            open: false,
            inputText: '',
            typing: false,
            showKeyConfig: false,
            geminiKey: localStorage.getItem('ray_gemini_api_key') || ''
        };

        $scope.toggleGeminiKeyConfig = function () {
            $scope.aiState.showKeyConfig = !$scope.aiState.showKeyConfig;
        };

        $scope.saveGeminiKey = function () {
            var k = ($scope.aiState.geminiKey || '').trim();
            localStorage.setItem('ray_gemini_api_key', k);
            $scope.aiState.showKeyConfig = false;
            if (k) {
                $scope.showToast('Google Gemini Free API Anahtarı kaydedildi!', false);
            } else {
                $scope.showToast('Otomatik Ücretsiz Yapay Zeka modu aktif.', false);
            }
        };

        $scope.aiMessages = [
            {
                sender: 'bot',
                html: $sce.trustAsHtml('Merhaba! Ben <strong>Gemini</strong> destekli <strong>RAY Yapay Zeka Asistanınızım</strong> 🤖<br>Bana hem <strong>masa/oda özellikleri, rezervasyon ve Check-In</strong> işlemlerini söyleyebilir hem de <strong>masa harici merak ettiğiniz her türlü soruyu</strong> sorabilirsiniz:'),
                buttons: [
                    { label: '✨ Bana Bugün Boş Masa Ayırt', command: 'Bana bugün boş masa ayırt', style: 'blue' },
                    { label: '🖥️ Çift Monitörlü Masalar Hangileri?', command: 'Çift monitörlü ve cam kenarı boş masalar hangileri?', style: 'outline' },
                    { label: '📍 Ofisteyim, Check-In Yap', command: 'Ofise geldim, Check-In yap', style: 'blue' },
                    { label: '👥 Bugün Ofiste Kimler Var?', command: 'Bugün ofiste kimler var?', style: 'outline' }
                ]
            }
        ];

        $scope.toggleAiChat = function () {
            $scope.aiState.open = !$scope.aiState.open;
            if ($scope.aiState.open) {
                scrollAiChatBottom();
                $timeout(function () {
                    var inp = document.getElementById('aiChatInputField');
                    if (inp) inp.focus();
                }, 80);
            }
        };

        function scrollAiChatBottom() {
            $timeout(function () {
                var el = document.getElementById('aiChatMessagesBox');
                if (el) el.scrollTop = el.scrollHeight;
            }, 50);
        }

        $scope.clickAiActionButton = function (btn) {
            if (!btn || !btn.command) return;
            sendMessageToAi(btn.command, btn.label, !!btn.replaceExisting);
        };

        $scope.sendAiMessage = function () {
            var rawText = ($scope.aiState.inputText || '').trim();
            if (!rawText || !$scope.currentUser) return;
            $scope.aiState.inputText = '';
            var inp = document.getElementById('aiChatInputField');
            if (inp) inp.value = '';
            sendMessageToAi(rawText, rawText, false);
        };

        function formatMarkdownClient(text) {
            if (!text) return '';
            var safe = String(text)
                .replace(/&/g, '&amp;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;');
            safe = safe.replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>');
            safe = safe.replace(/`([^`]+)`/g, '<code style="background:rgba(1,53,235,0.08);padding:1px 5px;border-radius:4px;font-weight:700;">$1</code>');
            safe = safe.replace(/\r\n/g, '\n').replace(/\n/g, '<br>');
            return safe;
        }

        function buildClientSitePrompt(targetDate) {
            var user = $scope.currentUser || { fullName: 'Misafir', department: 'Ofis', title: 'Çalışan', role: 'Employee' };
            var deskLines = ($scope.workspaces || []).map(function (w) {
                var status = w.isReserved ? 'DOLU' : 'BOŞ';
                var feats = (w.features || []).join(', ');
                return '- ' + w.code + ' (' + w.name + ', Tip: ' + w.type + ', Kapasite: ' + w.capacity + '): Durum=' + status + ' | Özellikler: [' + feats + ']';
            }).join('\n');

            var myActive = ($scope.myReservations || []).map(function (r) {
                return r.date + ' -> ' + r.workspaceCode + ' (' + r.status + ')';
            }).join('; ') || 'Yok';

            var checkedInPeople = ($scope.peopleInOffice || []).filter(function (p) {
                return p.status === 'CheckedIn';
            }).map(function (p) {
                return p.fullName + ' (' + p.workspaceCode + ')';
            }).join(', ') || 'Henüz kimse Check-In yapmadı';

            return [
                "Sen 'RAY Yapay Zeka Asistanı'sın (Google Gemini altyapısıyla çalışıyorsun).",
                "Hem içinde bulunduğun 'RAY OFİS — Masa & Toplantı Odası Rezervasyon Sistemi' web uygulamasını eksiksiz biliyorsun, hem de kullanıcının masa/ofis dışındaki her türlü genel kültür, yazılım, günlük hayat, matematik, yemek tarifi, tavsiye veya sohbet sorularına zekice, samimi ve doğru Türkçe yanıtlar veriyorsun.",
                "",
                "=== KULLANICI VE TARİH BİLGİSİ ===",
                "- Konuştuğun Kullanıcı: " + user.fullName + " (" + user.department + " - " + user.title + ")",
                "- Bugünün Tarihi: " + $scope.todayDate + " | Seçili Tarih: " + targetDate,
                "- Kullanıcının Aktif Rezervasyonları: " + myActive,
                "- Bugün Ofiste Check-In Yapmış Olanlar: " + checkedInPeople,
                "",
                "=== SİTE KURALLARI VE ÖZELLİKLERİ (RAY OFİS) ===",
                "1. Ofiste 10 adet 2 kişilik masa modülü (1A, 1B ... 10A, 10B toplam 20 koltuk) ve 4 adet Toplantı Odası (ODA-1, ODA-2, ODA-3, ODA-4) vardır.",
                "2. Bir kullanıcı aynı gün içinde en fazla 1 masa ve 1 toplantı odası rezerve edebilir. Aynı güne 2. bir masa veya oda seçerse sistem eski rezervasyonunu otomatik iptal edip yenisiyle değiştirir.",
                "3. Check-In (Ofisteyim doğrulaması) SADECE rezervasyonun olduğu gün (bugün) yapılabilir. Yarın veya ileri tarihli masalar önceden Check-In yapılamaz.",
                "4. Gizlilik gereği normal kullanıcılar rezervasyon ekranında kırmızı (dolu) koltukları kimin aldığını göremez; yalnızca ofise gelip Check-In yapanları 'Ofistekiler' sekmesinde görebilir.",
                "5. Rezervasyon yapıldığında Google Takvim ve Outlook Takvim'e ekleme butonları çıkar.",
                "",
                "=== " + targetDate + " TARİHİ İÇİN MASALAR, ODALAR VE DONANIM ÖZELLİKLERİ ===",
                deskLines,
                "",
                "=== YANIT FORMATI KURALLARI ===",
                "- Yanıtlarını her zaman Türkçe, kısa, net ve okunaklı ver (en fazla 3-5 cümle veya maddeli liste).",
                "- Önemli kelimeleri ve masa numaralarını (örn. **1A**, **3B**, **ODA-1**) kalın (**...**) yaz.",
                "- Eğer kullanıcı masa/ofis dışında genel bir soru sorarsa, doğrudan o soruyu en iyi şekilde yanıtla."
            ].join('\n');
        }

        function buildDynamicSeatButtonsClient(htmlText, targetDate) {
            var btns = [];
            var seen = {};
            var regex = /\b(10[AB]|[1-9][AB]|ODA-[1-4])\b/g;
            var match;
            while ((match = regex.exec(htmlText)) !== null) {
                var code = match[1].toUpperCase();
                if (seen[code]) continue;
                var ws = ($scope.workspaces || []).find(function (w) { return w.code === code; });
                if (ws && !ws.isReserved) {
                    seen[code] = true;
                    btns.push({
                        label: '🪑 ' + code + ' Seç',
                        command: targetDate + ' için ' + code + ' koltuğunu ayırt',
                        style: 'blue'
                    });
                    if (btns.length >= 6) break;
                }
            }
            if (btns.length === 0) {
                btns.push({ label: '✨ Bugün Boş Masa Seç', command: 'Bana bugün boş masa ayırt', style: 'blue' });
                btns.push({ label: '📍 Check-In Yap', command: 'Ofise geldim, Check-In yap', style: 'outline' });
                btns.push({ label: '👥 Ofiste Kimler Var?', command: 'Bugün ofiste kimler var?', style: 'outline' });
            }
            return btns;
        }

        function callBrowserGeminiFree(systemPrompt, userMessage, targetDate, fallbackCallback) {
            var apiKey = ($scope.aiState.geminiKey || '').trim();

            function tryPollinationsFree() {
                fetch('https://text.pollinations.ai/openai', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        model: 'openai',
                        messages: [
                            { role: 'system', content: systemPrompt },
                            { role: 'user', content: userMessage }
                        ],
                        temperature: 0.7
                    })
                })
                .then(function (r) { return r.json(); })
                .then(function (d) {
                    var content = d && d.choices && d.choices[0] && d.choices[0].message && d.choices[0].message.content;
                    if (content) {
                        var html = formatMarkdownClient(content);
                        $scope.$applyAsync(function () {
                            $scope.aiState.typing = false;
                            handleAiResponse({
                                replyHtml: html,
                                action: 'GEMINI_REPLY',
                                targetDate: targetDate,
                                buttons: buildDynamicSeatButtonsClient(html, targetDate)
                            });
                        });
                    } else {
                        fallbackCallback();
                    }
                })
                .catch(function () {
                    fallbackCallback();
                });
            }

            if (apiKey) {
                var gemUrl = 'https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=' + encodeURIComponent(apiKey);
                fetch(gemUrl, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        system_instruction: { parts: [{ text: systemPrompt }] },
                        contents: [{ role: 'user', parts: [{ text: userMessage }] }],
                        generationConfig: { temperature: 0.7, maxOutputTokens: 500 }
                    })
                })
                .then(function (r) { return r.json(); })
                .then(function (d) {
                    var textVal = d && d.candidates && d.candidates[0] && d.candidates[0].content && d.candidates[0].content.parts && d.candidates[0].content.parts[0] && d.candidates[0].content.parts[0].text;
                    if (textVal) {
                        var html = formatMarkdownClient(textVal);
                        $scope.$applyAsync(function () {
                            $scope.aiState.typing = false;
                            handleAiResponse({
                                replyHtml: html,
                                action: 'GEMINI_REPLY',
                                targetDate: targetDate,
                                buttons: buildDynamicSeatButtonsClient(html, targetDate)
                            });
                        });
                    } else {
                        tryPollinationsFree();
                    }
                })
                .catch(function () {
                    tryPollinationsFree();
                });
            } else {
                tryPollinationsFree();
            }
        }

        function sendMessageToAi(commandText, displayLabel, replaceExisting) {
            var nowTime = new Date().toTimeString().slice(0, 5);
            $scope.aiMessages.push({
                sender: 'user',
                text: displayLabel || commandText,
                time: nowTime
            });
            $scope.aiState.typing = true;
            scrollAiChatBottom();

            var payload = {
                userId: $scope.currentUser.id,
                message: commandText,
                selectedDate: $scope.selectedDate,
                replaceExisting: !!replaceExisting,
                geminiApiKey: ($scope.aiState.geminiKey || '').trim()
            };

            $http.post('/api/ai/chat', payload)
                .then(function (res) {
                    var data = res.data;
                    if (data && data.usedOfflineFallback) {
                        // Backend couldn't reach external network; try direct browser Gemini/Free LLM call
                        var sysPrompt = data.systemPrompt || buildClientSitePrompt(data.targetDate || $scope.selectedDate);
                        callBrowserGeminiFree(sysPrompt, commandText, data.targetDate || $scope.selectedDate, function () {
                            $scope.$applyAsync(function () {
                                $scope.aiState.typing = false;
                                handleAiResponse(data);
                            });
                        });
                        return;
                    }
                    $timeout(function () {
                        $scope.aiState.typing = false;
                        handleAiResponse(data);
                    }, 180);
                })
                .catch(function () {
                    var localReply = processLocalAiChat(payload);
                    if (localReply && localReply.needsGeminiBrowserCall) {
                        var sysPrompt = buildClientSitePrompt(localReply.targetDate);
                        callBrowserGeminiFree(sysPrompt, commandText, localReply.targetDate, function () {
                            $scope.$applyAsync(function () {
                                $scope.aiState.typing = false;
                                handleAiResponse(localReply);
                            });
                        });
                        return;
                    }
                    $timeout(function () {
                        $scope.aiState.typing = false;
                        handleAiResponse(localReply);
                    }, 180);
                });
        }

        function handleAiResponse(data) {
            if (!data) return;
            var htmlContent = data.replyHtml || data.reply || '';
            $scope.aiMessages.push({
                sender: 'bot',
                html: $sce.trustAsHtml(htmlContent),
                buttons: data.buttons || [],
                reservedItem: data.reservedItem || null
            });

            if (data.showPopup) {
                $scope.openResultPopup(data.popupTitle, data.popupMessage, data.reservedItem || null);
            }

            if (data.targetDate && data.targetDate !== $scope.selectedDate) {
                $scope.selectDate(data.targetDate);
            } else {
                loadWorkspaces();
                loadMyReservations();
                if ($scope.currentUser && $scope.currentUser.role === 'Admin') {
                    loadAdminDashboard();
                }
            }
            scrollAiChatBottom();
        }

        // ==================== LOCAL AI FALLBACK (GITHUB PAGES) ====================
        function processLocalAiChat(req) {
            var db = getFallbackStore();
            var user = $scope.currentUser;
            var lower = (req.message || '').toLocaleLowerCase('tr-TR');
            var todayStr = $scope.todayDate;

            var targetDate = req.selectedDate || todayStr;
            var today = new Date();
            if (lower.indexOf('bugün') !== -1 || lower.indexOf('bugun') !== -1) {
                targetDate = todayStr;
            } else if (lower.indexOf('öbür gün') !== -1 || lower.indexOf('2 gün sonra') !== -1) {
                var d2 = new Date(today); d2.setDate(today.getDate() + 2);
                targetDate = formatDateISO(d2);
            } else if (lower.indexOf('yarın') !== -1 || lower.indexOf('yarin') !== -1) {
                var d1 = new Date(today); d1.setDate(today.getDate() + 1);
                targetDate = formatDateISO(d1);
            } else {
                var dm = lower.match(/\b(202\d-\d{2}-\d{2})\b/);
                if (dm) targetDate = dm[1];
            }

            if (lower.indexOf('vazgeç') !== -1 && lower.indexOf('iptal') === -1) {
                return {
                    replyHtml: 'İşlemden vazgeçildi, mevcut rezervasyonunuz korundu.',
                    action: 'NONE',
                    targetDate: targetDate,
                    buttons: [{ label: '📅 Rezervasyonlarımı Göster', command: 'Rezervasyonlarım neler?', style: 'outline' }]
                };
            }

            var isQuestionAboutRules =
                lower.indexOf('nasıl') !== -1 || lower.indexOf('nasil') !== -1 ||
                lower.indexOf('nedir') !== -1 || lower.indexOf('kural') !== -1 ||
                lower.indexOf('ne işe') !== -1 || lower.indexOf('hakkında') !== -1;

            var timeSlot = 'Tam Gün (09:00 - 18:00)';
            if (lower.indexOf('öğleden önce') !== -1 || lower.indexOf('sabah') !== -1) timeSlot = 'Öğleden Önce (09:00 - 13:00)';
            else if (lower.indexOf('öğleden sonra') !== -1) timeSlot = 'Öğleden Sonra (13:30 - 18:00)';

            var matchedWs = null;
            var roomMatch = lower.match(/oda[\s\-]*([1-4])/);
            if (roomMatch) {
                var rCode = 'ODA-' + roomMatch[1];
                matchedWs = db.workspaces.find(function (w) { return w.code === rCode; });
            } else {
                var seatMatch = lower.match(/\b(10|[1-9])\s*([ab])\b/);
                if (seatMatch) {
                    var sCode = (seatMatch[1] + seatMatch[2]).toUpperCase();
                    matchedWs = db.workspaces.find(function (w) { return w.code === sCode; });
                }
            }

            // Check-In (TODAY ONLY!)
            if (!isQuestionAboutRules && (lower.indexOf('check') !== -1 || lower.indexOf('ofisteyim') !== -1 || lower.indexOf('ofise geldim') !== -1 || lower.indexOf('doğrula') !== -1)) {
                var pendingToday = db.reservations.find(function (r) {
                    return r.userId === user.id && r.date === todayStr && r.status === 'Active';
                });
                if (pendingToday) {
                    pendingToday.status = 'CheckedIn';
                    saveFallbackStore(db);
                    var pWs = db.workspaces.find(function (w) { return w.id === pendingToday.workspaceItemId; });
                    return {
                        replyHtml: '✅ Harika <strong>' + user.fullName + '</strong>! Bugünkü (<strong>' + todayStr + '</strong>) <strong>' + (pWs ? pWs.code : '') + '</strong> rezervasyonunuz için <strong>Ofis Check-In</strong> doğrulamanızı tamamladım.',
                        action: 'CHECKED_IN',
                        targetDate: todayStr,
                        showPopup: true,
                        popupTitle: 'Ofis Check-In Doğrulandı!',
                        popupMessage: (pWs ? pWs.code : '') + ' için bugün ofiste olduğunuz doğrulandı.',
                        buttons: [{ label: '👥 Ofiste Kimler Var?', command: 'Bugün ofiste kimler var?', style: 'blue' }]
                    };
                }
                var futureR = db.reservations.find(function (r) {
                    return r.userId === user.id && r.status === 'Active' && r.date > todayStr;
                });
                if (futureR) {
                    var fWs = db.workspaces.find(function (w) { return w.id === futureR.workspaceItemId; });
                    return {
                        replyHtml: '⚠️ <strong>' + futureR.date + '</strong> tarihli <strong>' + (fWs ? fWs.code : '') + '</strong> rezervasyonunuz ileri bir tarihe ait. <strong>Check-In işlemi yalnızca aynı gün içinde (' + todayStr + ') yapılabilir.</strong>',
                        action: 'NONE',
                        targetDate: todayStr,
                        buttons: [{ label: '✨ Bugün İçin Masa Seç', command: 'Bana bugün boş masa ayırt', style: 'blue' }]
                    };
                }
                return {
                    replyHtml: 'Bugün (<strong>' + todayStr + '</strong>) için Check-In bekleyen aktif bir masa rezervasyonunuz bulunmuyor.',
                    action: 'NONE',
                    targetDate: todayStr,
                    buttons: [{ label: '✨ Bugün Boş Masaları Göster', command: 'Bana bugün boş masa ayırt', style: 'blue' }]
                };
            }

            // Cancel
            if (!isQuestionAboutRules && (lower.indexOf('iptal') !== -1 || lower.indexOf('rezervasyonumu sil') !== -1)) {
                var activeR = db.reservations.slice().reverse().find(function (r) { return r.userId === user.id && r.status !== 'Cancelled'; });
                if (activeR) {
                    activeR.status = 'Cancelled';
                    saveFallbackStore(db);
                    var cWs = db.workspaces.find(function (w) { return w.id === activeR.workspaceItemId; });
                    return {
                        replyHtml: '🗑️ <strong>' + activeR.date + '</strong> tarihli <strong>' + (cWs ? cWs.code : '') + '</strong> rezervasyonunuz iptal edildi.',
                        action: 'CANCELLED',
                        targetDate: activeR.date,
                        showPopup: true,
                        popupTitle: 'Rezervasyon İptal Edildi',
                        popupMessage: activeR.date + ' tarihli ' + (cWs ? cWs.code : '') + ' rezervasyonunuz iptal edildi.',
                        buttons: [{ label: '✨ Yeni Masa Seç', command: 'Bana bugün boş masa ayırt', style: 'blue' }]
                    };
                }
                return {
                    replyHtml: 'İptal edilebilecek aktif bir rezervasyonunuz bulunmuyor.',
                    action: 'NONE',
                    targetDate: targetDate,
                    buttons: [{ label: '✨ Bana Boş Masa Ayırt', command: 'Bana bugün boş masa ayırt', style: 'blue' }]
                };
            }

            // Who is in office
            if (lower.indexOf('kimler var') !== -1 || lower.indexOf('kim var') !== -1 || lower.indexOf('ofistekiler') !== -1) {
                var checkedPeople = db.reservations
                    .filter(function (r) { return r.date === targetDate && r.status === 'CheckedIn'; })
                    .map(function (r) {
                        var u = db.users.find(function (x) { return x.id === r.userId; });
                        var w = db.workspaces.find(function (x) { return x.id === r.workspaceItemId; });
                        return '<strong>' + (u ? u.fullName : 'Çalışan') + '</strong> (<strong>' + (w ? w.code : '') + '</strong>)';
                    });
                return {
                    replyHtml: checkedPeople.length > 0
                        ? '<strong>' + targetDate + '</strong> tarihinde ofiste Check-In yapmış <strong>' + checkedPeople.length + ' kişi</strong> var:<br>• ' + checkedPeople.join('<br>• ')
                        : '<strong>' + targetDate + '</strong> tarihinde henüz ofiste Check-In yapmış kimse görünmüyor.',
                    action: 'SHOW_PRESENCE',
                    targetDate: targetDate,
                    buttons: [{ label: '✨ Bana Boş Masa Ayırt', command: 'Bana bugün boş masa ayırt', style: 'blue' }]
                };
            }

            var isAskingFeatures =
                lower.indexOf('özellik') !== -1 || lower.indexOf('ozellik') !== -1 ||
                lower.indexOf('ne var') !== -1 || lower.indexOf('nedir') !== -1 ||
                lower.indexOf('monitör') !== -1 || lower.indexOf('monitor') !== -1 ||
                lower.indexOf('cam kenar') !== -1 || lower.indexOf('yükseklik') !== -1;

            var hasBookingVerb =
                lower.indexOf('ayırt') !== -1 || lower.indexOf('ayirt') !== -1 ||
                lower.indexOf('rezerv') !== -1 || lower.indexOf('seç') !== -1 ||
                lower.indexOf('değiştir') !== -1 || lower.indexOf('degistir') !== -1 ||
                lower.trim().length <= 4;

            var dayBusyIds = db.reservations
                .filter(function (r) { return r.date === targetDate && r.status !== 'Cancelled'; })
                .map(function (r) { return r.workspaceItemId; });

            if (matchedWs && (!isAskingFeatures || hasBookingVerb)) {
                if (dayBusyIds.indexOf(matchedWs.id) !== -1) {
                    var alts = db.workspaces.filter(function (w) { return w.type === matchedWs.type && dayBusyIds.indexOf(w.id) === -1; });
                    var altBold = alts.map(function (a) { return '<strong>' + a.code + '</strong>'; }).join(', ');
                    return {
                        replyHtml: 'Üzgünüm, <strong>' + targetDate + '</strong> tarihinde <strong>' + matchedWs.code + '</strong> dolu.<br>Boş olanlar: ' + altBold + '<br>Hangisini istersiniz?',
                        action: 'NONE',
                        targetDate: targetDate,
                        buttons: alts.slice(0, 8).map(function (a) {
                            return { label: '🪑 ' + a.code + ' Seç', command: targetDate + ' için ' + a.code + ' koltuğunu ayırt', style: 'blue' };
                        })
                    };
                }

                // 1 item per day per type ("Masa" or "Oda") with automatic cancellation of all previous ones on swap
                var existingSameDayList = db.reservations.filter(function (r) {
                    if (r.userId !== user.id || r.date !== targetDate || r.status === 'Cancelled') return false;
                    var w = db.workspaces.find(function (x) { return x.id === r.workspaceItemId; });
                    return w && w.type === matchedWs.type;
                });

                var shouldReplace = req.replaceExisting || lower.indexOf('değiştir') !== -1 || lower.indexOf('degistir') !== -1;
                var typeLabel = matchedWs.type === 'Oda' ? 'toplantı odası' : 'masa';

                if (existingSameDayList.length > 0 && !shouldReplace) {
                    var oldCodesList = existingSameDayList.map(function (r) {
                        var w = db.workspaces.find(function (x) { return x.id === r.workspaceItemId; });
                        return w ? w.code : '';
                    }).join(', ');

                    return {
                        replyHtml: '⚠️ <strong>' + targetDate + '</strong> tarihinde zaten <strong>' + oldCodesList + '</strong> (' + typeLabel + ') rezervasyonunuz var.<br>Eski rezervasyonunuzu otomatik iptal edip <strong>' + matchedWs.code + '</strong> ile <strong>değiştirmek ister misiniz?</strong>',
                        action: 'ASK_CHANGE',
                        targetDate: targetDate,
                        buttons: [
                            { label: '🔄 Evet, ' + matchedWs.code + ' İle Değiştir', command: targetDate + ' için ' + matchedWs.code + ' koltuğunu değiştir', style: 'blue', replaceExisting: true },
                            { label: '✕ Hayır, ' + oldCodesList + ' Kalsın', command: 'Değiştirmekten vazgeç', style: 'outline' }
                        ]
                    };
                }

                var cancelledCodes = null;
                if (existingSameDayList.length > 0 && shouldReplace) {
                    cancelledCodes = existingSameDayList.map(function (r) {
                        var w = db.workspaces.find(function (x) { return x.id === r.workspaceItemId; });
                        return w ? w.code : '';
                    }).join(', ');
                    existingSameDayList.forEach(function (r) {
                        r.status = 'Cancelled';
                    });
                }

                var nextId = db.reservations.length > 0 ? Math.max.apply(null, db.reservations.map(function (r) { return r.id; })) + 1 : 1;
                db.reservations.push({
                    id: nextId,
                    workspaceItemId: matchedWs.id,
                    userId: user.id,
                    date: targetDate,
                    timeSlot: timeSlot,
                    note: 'RAY AI Asistan ile ayrıldı',
                    status: 'Active'
                });
                saveFallbackStore(db);

                var rItem = { code: matchedWs.code, name: matchedWs.name, date: targetDate, timeSlot: timeSlot };
                var btns = [];
                if (targetDate === todayStr) {
                    btns.push({ label: '📍 Ofisteyim — Check-In Yap', command: 'Ofise geldim, Check-In yap', style: 'blue' });
                }
                btns.push({ label: '🗑️ Rezervasyonu İptal Et', command: 'Son rezervasyonumu iptal et', style: 'red' });

                return {
                    replyHtml: cancelledCodes
                        ? '🔄 Eski rezervasyonunuz (<strong>' + cancelledCodes + '</strong>) otomatik iptal edildi ve <strong>' + targetDate + '</strong> günü için <strong>' + matchedWs.code + ' — ' + matchedWs.name + '</strong> adınıza ayrıldı!'
                        : '🎉 Tamamdır <strong>' + user.fullName + '</strong>! <strong>' + targetDate + '</strong> tarihi için <strong>' + matchedWs.code + ' — ' + matchedWs.name + '</strong> sizin adınıza ayrıldı!',
                    action: 'RESERVED',
                    targetDate: targetDate,
                    showPopup: true,
                    popupTitle: cancelledCodes ? 'Rezervasyon Değiştirildi!' : 'Rezervasyon Tamamlandı!',
                    popupMessage: cancelledCodes
                        ? 'Eski rezervasyonunuz (' + cancelledCodes + ') iptal edildi ve ' + targetDate + ' günü için ' + matchedWs.code + ' (' + matchedWs.name + ') adınıza tanımlandı.'
                        : targetDate + ' günü (' + timeSlot + ') için ' + matchedWs.code + ' (' + matchedWs.name + ') adınıza ayrıldı.',
                    reservedItem: rItem,
                    buttons: btns
                };
            }

            var isDirectListCommand =
                (lower.indexOf('boş masa') !== -1 || lower.indexOf('bos masa') !== -1 ||
                 lower.indexOf('boş oda') !== -1 || lower.indexOf('bos oda') !== -1 ||
                 lower.indexOf('masa ayırt') !== -1 || lower.indexOf('masa ayirt') !== -1 ||
                 lower.indexOf('oda ayırt') !== -1 || lower.indexOf('oda ayirt') !== -1) &&
                !isAskingFeatures;

            if (isDirectListCommand) {
                var wantsRoom = lower.indexOf('oda') !== -1 || lower.indexOf('toplantı') !== -1;
                var freeItems = db.workspaces.filter(function (w) {
                    return w.type === (wantsRoom ? 'Oda' : 'Masa') && dayBusyIds.indexOf(w.id) === -1;
                });
                var boldCodes = freeItems.map(function (w) { return '<strong>' + w.code + '</strong>'; }).join(', ');
                return {
                    replyHtml: '📅 <strong>' + targetDate + '</strong> için boş ' + (wantsRoom ? 'oda' : 'masa') + ' numaraları:<br>' + boldCodes + '<br><strong>Hangisini istersiniz? Seçin:</strong>',
                    action: 'LIST_OPTIONS',
                    targetDate: targetDate,
                    buttons: freeItems.map(function (w) {
                        return { label: '🪑 ' + w.code + ' Seç', command: targetDate + ' için ' + w.code + ' koltuğunu ayırt', style: 'blue' };
                    })
                };
            }

            // Any other question (site knowledge, desk features, general knowledge, coding, daily life) -> Call Gemini Free AI!
            return {
                needsGeminiBrowserCall: true,
                replyHtml: 'Merhaba <strong>' + user.fullName + '</strong>! Size hem ofis masa/oda özellikleri ve rezervasyon işlemlerinde hem de merak ettiğiniz diğer tüm konularda yardımcı olabilirim.',
                action: 'GEMINI_REPLY',
                targetDate: targetDate,
                buttons: [
                    { label: '✨ Bugün Boş Masa Seç', command: 'Bana bugün boş masa ayırt', style: 'blue' },
                    { label: '📍 Check-In Yap', command: 'Ofise geldim, Check-In yap', style: 'outline' }
                ]
            };
        }

        // ==================== FALLBACK STORE WITH RICH DESK FEATURES ====================
        function buildFallbackFeatures(deskNo, isSeatA) {
            var list = [];
            if ((deskNo % 2 === 1 && isSeatA) || (deskNo % 2 === 0 && !isSeatA))
                list.push('Cam Kenarı Masa (Gün Işığı)');
            else
                list.push('Koridor Yanı (Kolay Erişim)');

            if (deskNo <= 4) list.push('Çift Monitör (2x27" IPS)');
            else if (deskNo <= 7) list.push('34" UltraWide Kavisli Monitör');
            else list.push('27" 4K Monitör + Laptop Standı');

            if (deskNo % 2 === 1) list.push('Yükseklik Ayarlanabilen Motorlu Masa');
            else list.push('Geniş Ahşap Çalışma Yüzeyi');

            list.push('Type-C Hızlı Şarj Dock & Ethernet');
            list.push('Ergonomik Bel Destekli Koltuk');
            return list;
        }

        function getFallbackStore() {
            var raw = localStorage.getItem('ray_ofis_static_db_v4');
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
                workspaces.push({ id: idCounter++, code: deskNo + 'A', name: 'Masa ' + deskNo + ' - Sol Koltuk (' + deskNo + 'A)', type: 'Masa', zone: 'Masa ' + deskNo, capacity: 1, features: buildFallbackFeatures(deskNo, true), gridX: side, gridY: row, gridW: deskNo, gridH: 1, isActive: true });
                workspaces.push({ id: idCounter++, code: deskNo + 'B', name: 'Masa ' + deskNo + ' - Sağ Koltuk (' + deskNo + 'B)', type: 'Masa', zone: 'Masa ' + deskNo, capacity: 1, features: buildFallbackFeatures(deskNo, false), gridX: side, gridY: row, gridW: deskNo, gridH: 2, isActive: true });
            }
            workspaces.push({ id: idCounter++, code: 'ODA-1', name: 'Toplantı Odası 1 (Sprint)', type: 'Oda', zone: 'Toplantı Odaları', capacity: 6, features: ['65" 4K Akıllı Ekran', 'Cam Yazı Tahtası', 'Ses Yalıtımlı Kabin', 'Kablosuz HDMI Sunum'], isActive: true });
            workspaces.push({ id: idCounter++, code: 'ODA-2', name: 'Toplantı Odası 2 (İnovasyon)', type: 'Oda', zone: 'Toplantı Odaları', capacity: 8, features: ['Polycom Video Konferans', 'Çift Projeksiyon', 'Cam Kenarı Manzaralı', 'Akustik Panel'], isActive: true });
            workspaces.push({ id: idCounter++, code: 'ODA-3', name: 'Toplantı Odası 3 (Hızlı Görüşme)', type: 'Oda', zone: 'Toplantı Odaları', capacity: 4, features: ['55" LED TV', 'Yükseklik Ayarlanabilir Masa', 'Hızlı Şarj İstasyonu'], isActive: true });
            workspaces.push({ id: idCounter++, code: 'ODA-4', name: 'Büyük Konferans Odası', type: 'Oda', zone: 'Toplantı Odaları', capacity: 12, features: ['85" İnteraktif Ekran', '360° Mikrofon & Ses Sistemi', 'Panoramik Cam Cephe', 'İkram & Kahve Köşesi'], isActive: true });

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
            localStorage.setItem('ray_ofis_static_db_v4', JSON.stringify(db));
            return db;
        }

        function saveFallbackStore(db) {
            localStorage.setItem('ray_ofis_static_db_v4', JSON.stringify(db));
        }

        $scope.showToast = function (message, isError) {
            var toast = { id: Date.now() + Math.random(), message: message, isError: !!isError };
            $scope.toasts.push(toast);
            $timeout(function () {
                var idx = $scope.toasts.indexOf(toast);
                if (idx !== -1) $scope.toasts.splice(idx, 1);
            }, 3200);
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
                        status: item.reservation.status
                    });
                }

                if (item.type === 'Oda') {
                    rooms.push(item);
                } else {
                    var deskNo = item.gridW || 1;
                    if (!desksMap[deskNo]) {
                        desksMap[deskNo] = {
                            deskNo: deskNo,
                            title: 'MASA ' + deskNo,
                            specBadge: (deskNo % 2 === 1) ? 'Yükseklik Ayarlı' : 'Geniş Yüzey',
                            monitorBadge: (deskNo <= 4) ? 'Çift Monitör' : (deskNo <= 7 ? '34" UltraWide' : '4K Ekran'),
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

        // ==================== CREATE / REPLACE RESERVATION (BOTH DESKS & ROOMS) ====================
        $scope.createReservation = function () {
            if (!$scope.selectedItem || !$scope.currentUser) return;

            var payload = {
                workspaceItemId: $scope.selectedItem.id,
                userId: $scope.currentUser.id,
                date: $scope.selectedDate,
                timeSlot: $scope.bookingForm.timeSlot,
                note: $scope.bookingForm.note,
                replaceExisting: false
            };

            executeCreateReservation(payload);
        };

        function executeCreateReservation(payload) {
            $http.post('/api/reservations', payload)
                .then(function (res) {
                    var data = res.data;
                    if (data && data.requiresChangeConfirmation) {
                        $scope.changeConfirmPopup = {
                            visible: true,
                            existingCode: data.existingCode,
                            newWorkspaceId: data.newWorkspaceId,
                            newCode: data.newCode,
                            newName: data.newName,
                            itemType: data.itemType || 'Masa',
                            date: data.date,
                            timeSlot: data.timeSlot,
                            note: payload.note,
                            message: data.message
                        };
                        return;
                    }
                    if (data && !data.success) {
                        $scope.showToast(data.message, true);
                        return;
                    }
                    $scope.bookingForm.note = '';
                    var reservedInfo = {
                        code: data.reservation ? data.reservation.workspaceCode : $scope.selectedItem.code,
                        name: data.reservation ? data.reservation.workspaceName : $scope.selectedItem.name,
                        date: payload.date,
                        timeSlot: payload.timeSlot
                    };
                    $scope.openResultPopup(
                        data.replaced ? 'Rezervasyon Değiştirildi!' : 'Rezervasyon Tamamlandı!',
                        data.message,
                        reservedInfo
                    );
                    loadWorkspaces();
                    loadMyReservations();
                    if ($scope.currentUser.role === 'Admin') loadAdminDashboard();
                })
                .catch(function () {
                    var db = getFallbackStore();
                    var targetWs = db.workspaces.find(function (w) { return w.id === payload.workspaceItemId; });
                    if (!targetWs) return;

                    var existingSameDayList = db.reservations.filter(function (r) {
                        if (r.userId !== payload.userId || r.date !== payload.date || r.status === 'Cancelled') return false;
                        var w = db.workspaces.find(function (x) { return x.id === r.workspaceItemId; });
                        return w && w.type === targetWs.type;
                    });

                    if (existingSameDayList.length > 0 && !payload.replaceExisting) {
                        var oldCodes = existingSameDayList.map(function (r) {
                            var w = db.workspaces.find(function (x) { return x.id === r.workspaceItemId; });
                            return w ? w.code : '';
                        }).join(', ');
                        $scope.changeConfirmPopup = {
                            visible: true,
                            existingCode: oldCodes,
                            newWorkspaceId: targetWs.id,
                            newCode: targetWs.code,
                            newName: targetWs.name,
                            itemType: targetWs.type,
                            date: payload.date,
                            timeSlot: payload.timeSlot,
                            note: payload.note,
                            message: payload.date + ' tarihinde zaten ' + oldCodes + ' (' + (targetWs.type === 'Oda' ? 'toplantı odası' : 'masa') + ') rezervasyonunuz bulunuyor. Eski rezervasyonunuzu otomatik iptal edip ' + targetWs.code + ' ile değiştirmek ister misiniz?'
                        };
                        return;
                    }

                    var cancelledOld = null;
                    if (existingSameDayList.length > 0 && payload.replaceExisting) {
                        cancelledOld = existingSameDayList.map(function (r) {
                            var w = db.workspaces.find(function (x) { return x.id === r.workspaceItemId; });
                            return w ? w.code : '';
                        }).join(', ');
                        existingSameDayList.forEach(function (r) {
                            r.status = 'Cancelled';
                        });
                    }

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

                    $scope.openResultPopup(
                        cancelledOld ? 'Rezervasyon Değiştirildi!' : 'Rezervasyon Tamamlandı!',
                        cancelledOld
                            ? 'Eski rezervasyonunuz (' + cancelledOld + ') otomatik iptal edildi ve ' + payload.date + ' günü için ' + targetWs.code + ' (' + targetWs.name + ') adınıza tanımlandı!'
                            : payload.date + ' tarihli ' + targetWs.code + ' (' + targetWs.name + ') rezervasyonunuz başarıyla tamamlandı!',
                        { code: targetWs.code, name: targetWs.name, date: payload.date, timeSlot: payload.timeSlot }
                    );
                    loadWorkspaces();
                    loadMyReservations();
                    if ($scope.currentUser.role === 'Admin') loadAdminDashboard();
                });
        }

        // ==================== CHECK-IN (TODAY ONLY) ====================
        $scope.checkInReservation = function (reservationId, resDate) {
            if (!$scope.currentUser) return;
            if (resDate && resDate !== $scope.todayDate) {
                $scope.showToast('Check-In işlemi yalnızca aynı gün (' + $scope.todayDate + ') içindeki rezervasyonlar için yapılabilir!', true);
                return;
            }
            $http.post('/api/reservations/' + reservationId + '/checkin?userId=' + $scope.currentUser.id, {})
                .then(function (res) {
                    $scope.openResultPopup('Ofis Check-In Doğrulandı!', res.data.message, null);
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
                    var r = db.reservations.find(function (x) { return x.id === reservationId; });
                    if (r) {
                        if (r.date !== $scope.todayDate) {
                            $scope.showToast('Check-In yalnızca rezervasyon gününde (' + r.date + ') yapılabilir!', true);
                            return;
                        }
                        r.status = 'CheckedIn';
                    }
                    saveFallbackStore(db);
                    $scope.openResultPopup('Ofis Check-In Doğrulandı!', 'Bugünkü Ofis Check-In işleminiz başarıyla doğrulandı!', null);
                    loadWorkspaces();
                    loadMyReservations();
                    if ($scope.currentUser.role === 'Admin') loadAdminDashboard();
                });
        };

        $scope.cancelReservation = function (reservationId) {
            if (!$scope.currentUser) return;
            $http.delete('/api/reservations/' + reservationId + '?userId=' + $scope.currentUser.id)
                .then(function (res) {
                    $scope.openResultPopup('Rezervasyon İptal Edildi', res.data.message, null);
                    loadWorkspaces();
                    loadMyReservations();
                    if ($scope.currentUser.role === 'Admin') loadAdminDashboard();
                })
                .catch(function () {
                    var db = getFallbackStore();
                    var r = db.reservations.find(function (x) { return x.id === reservationId; });
                    if (r) r.status = 'Cancelled';
                    saveFallbackStore(db);
                    $scope.openResultPopup('Rezervasyon İptal Edildi', 'Rezervasyonunuz iptal edildi ve koltuk/oda tekrar boşa çıkarıldı.', null);
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
