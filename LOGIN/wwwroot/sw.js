const CACHE_NAME = 'inventario-pwa-v1';
const ASSETS = [
    '/',
    '/css/site.css',
    '/js/site.js',
    'https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css',
    'https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css',
    'https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap'
];

// Instalar el Service Worker y almacenar en caché el "esqueleto" de la app
self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_NAME).then(cache => {
            return cache.addAll(ASSETS);
        })
    );
});

// Activar y limpiar cachés antiguos
self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(keys => {
            return Promise.all(
                keys.map(key => {
                    if (key !== CACHE_NAME) {
                        return caches.delete(key);
                    }
                })
            );
        })
    );
});

// Estrategia: Network First (Red primero, si falla va a Caché) 
// Ideal para sistemas con bases de datos dinámicas como tu inventario en Render
self.addEventListener('fetch', event => {
    // Evitar interceptar peticiones de bases de datos externas o POSTs de login/compras
    if (event.request.method !== 'GET' || event.request.url.includes('/api/')) {
        return;
    }

    event.respondWith(
        fetch(event.request)
            .then(response => {
                // Clonar y almacenar en caché las respuestas válidas dinámicamente
                if (response.status === 200 && event.request.url.startsWith(self.location.origin)) {
                    const responseClone = response.clone();
                    caches.open(CACHE_NAME).then(cache => {
                        cache.put(event.request, responseClone);
                    });
                }
                return response;
            })
            .catch(() => caches.match(event.request).then(cachedResponse => {
                return cachedResponse || caches.match('/');
            }))
    );
});