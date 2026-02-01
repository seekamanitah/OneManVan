// OneManVan PWA JavaScript
// Handles service worker registration and install prompt

// Service Worker Registration
if ('serviceWorker' in navigator) {
    window.addEventListener('load', () => {
        navigator.serviceWorker.register('/service-worker.js')
            .then((registration) => {
                console.log('[PWA] Service Worker registered with scope:', registration.scope);
                
                // Check for updates
                registration.addEventListener('updatefound', () => {
                    const newWorker = registration.installing;
                    console.log('[PWA] New service worker installing...');
                    
                    newWorker.addEventListener('statechange', () => {
                        if (newWorker.state === 'installed' && navigator.serviceWorker.controller) {
                            // New content available
                            showUpdateNotification();
                        }
                    });
                });
            })
            .catch((error) => {
                console.error('[PWA] Service Worker registration failed:', error);
            });
    });
}

// Install Prompt Handler
let deferredPrompt;
const installBanner = document.getElementById('pwa-install-banner');

window.addEventListener('beforeinstallprompt', (e) => {
    console.log('[PWA] Install prompt available');
    e.preventDefault();
    deferredPrompt = e;
    
    // Show install banner if not already installed
    if (!window.matchMedia('(display-mode: standalone)').matches) {
        showInstallBanner();
    }
});

window.addEventListener('appinstalled', () => {
    console.log('[PWA] App installed successfully');
    deferredPrompt = null;
    hideInstallBanner();
    
    // Track installation (analytics)
    if (typeof gtag !== 'undefined') {
        gtag('event', 'pwa_installed');
    }
});

// Install Banner Functions
function showInstallBanner() {
    const banner = document.getElementById('pwa-install-banner');
    if (banner) {
        banner.classList.add('show');
    }
}

function hideInstallBanner() {
    const banner = document.getElementById('pwa-install-banner');
    if (banner) {
        banner.classList.remove('show');
    }
}

// Called from install button
window.installPWA = async function() {
    if (!deferredPrompt) {
        console.log('[PWA] No install prompt available');
        return;
    }
    
    // Show the install prompt
    deferredPrompt.prompt();
    
    // Wait for user response
    const { outcome } = await deferredPrompt.userChoice;
    console.log('[PWA] User choice:', outcome);
    
    deferredPrompt = null;
    hideInstallBanner();
};

window.dismissInstallBanner = function() {
    hideInstallBanner();
    // Remember dismissal for this session
    sessionStorage.setItem('pwa-banner-dismissed', 'true');
};

// Update Notification
function showUpdateNotification() {
    // Create update notification
    const notification = document.createElement('div');
    notification.id = 'pwa-update-notification';
    notification.className = 'pwa-notification';
    notification.innerHTML = `
        <div class="pwa-notification-content">
            <span>A new version is available!</span>
            <button onclick="updateServiceWorker()" class="btn btn-sm btn-light">Update</button>
            <button onclick="this.parentElement.parentElement.remove()" class="btn btn-sm btn-link text-white">Later</button>
        </div>
    `;
    document.body.appendChild(notification);
    
    setTimeout(() => {
        notification.classList.add('show');
    }, 100);
}

window.updateServiceWorker = function() {
    if ('serviceWorker' in navigator) {
        navigator.serviceWorker.getRegistration().then((registration) => {
            if (registration && registration.waiting) {
                registration.waiting.postMessage({ type: 'SKIP_WAITING' });
            }
        });
    }
    window.location.reload();
};

// Detect standalone mode (installed PWA)
window.isPWAInstalled = function() {
    return window.matchMedia('(display-mode: standalone)').matches ||
           window.navigator.standalone === true;
};

// Online/Offline detection
window.addEventListener('online', () => {
    console.log('[PWA] Back online');
    document.body.classList.remove('offline');
    
    // Trigger sync if available
    if ('serviceWorker' in navigator && 'sync' in window.registration) {
        navigator.serviceWorker.ready.then((registration) => {
            registration.sync.register('sync-data');
        });
    }
});

window.addEventListener('offline', () => {
    console.log('[PWA] Gone offline');
    document.body.classList.add('offline');
});

// Check initial state
if (!navigator.onLine) {
    document.body.classList.add('offline');
}

console.log('[PWA] Script loaded. Installed:', window.isPWAInstalled());
