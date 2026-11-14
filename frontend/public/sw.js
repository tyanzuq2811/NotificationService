// Service Worker for Native Windows Web Push Notifications
// Microservices CQRS Notification Platform

self.addEventListener('install', (event) => {
  self.skipWaiting();
});

self.addEventListener('activate', (event) => {
  event.waitUntil(self.clients.claim());
});

self.addEventListener('push', function (event) {
  let data = {};
  if (event.data) {
    try {
      data = event.data.json();
    } catch (e) {
      data = { title: 'Thông Báo Microservices', body: event.data.text() };
    }
  }

  const title = data.title || 'Hệ Thống Thông Báo Microservices';
  const options = {
    body: data.body || 'Bạn có thông báo mới từ hệ thống phân tán CQRS.',
    icon: data.icon || '/favicon.svg',
    badge: data.badge || '/favicon.svg',
    vibrate: [200, 100, 200],
    tag: data.data?.notificationId || 'notification-' + Date.now(),
    data: data.data || {},
    actions: [
      { action: 'open', title: 'Xem Chi Tiết' },
      { action: 'dismiss', title: 'Đóng' }
    ]
  };

  event.waitUntil(self.registration.showNotification(title, options));
});

self.addEventListener('notificationclick', function (event) {
  event.notification.close();

  if (event.action === 'dismiss') {
    return;
  }

  const targetUrl = 'http://localhost:5173';
  event.waitUntil(
    clients.matchAll({ type: 'window', includeUncontrolled: true }).then(function (clientList) {
      for (let i = 0; i < clientList.length; i++) {
        const client = clientList[i];
        if (client.url.includes('localhost:5173') && 'focus' in client) {
          return client.focus();
        }
      }
      if (clients.openWindow) {
        return clients.openWindow(targetUrl);
      }
    })
  );
});
