// Theme Manager - Handles dark/light mode switching with localStorage persistence
window.themeManager = {
    // Get current theme (returns true for dark, false for light)
    getTheme: function () {
        const savedTheme = localStorage.getItem('theme');
        
        // If no saved preference, check system preference
        if (!savedTheme) {
            const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
            return prefersDark;
        }
        
        return savedTheme === 'dark';
    },

    // Set theme (pass true for dark, false for light)
    setTheme: function (isDark) {
        const theme = isDark ? 'dark' : 'light';
        
        // Save to localStorage
        localStorage.setItem('theme', theme);
        
        // Apply theme to document
        this.applyTheme(isDark);
    },

    // Apply theme classes to document
    applyTheme: function (isDark) {
        const html = document.documentElement;
        
        if (isDark) {
            html.setAttribute('data-bs-theme', 'dark');
            html.classList.add('dark-theme');
            html.classList.remove('light-theme');
        } else {
            html.setAttribute('data-bs-theme', 'light');
            html.classList.add('light-theme');
            html.classList.remove('dark-theme');
        }
    },

    // Initialize theme on page load
    init: function () {
        const isDark = this.getTheme();
        this.applyTheme(isDark);
    }
};

// Initialize theme immediately when script loads (before Blazor starts)
window.themeManager.init();

// Debounce utility for search inputs
window.debounceHelper = {
    timers: {},
    
    // Call a .NET method after a delay (debounced)
    // dotNetRef: DotNetObjectReference, methodName: string, delay: number
    debounce: function (timerId, dotNetRef, methodName, delay, value) {
        if (this.timers[timerId]) {
            clearTimeout(this.timers[timerId]);
        }
        
        this.timers[timerId] = setTimeout(() => {
            dotNetRef.invokeMethodAsync(methodName, value);
            delete this.timers[timerId];
        }, delay);
    },
    
    // Clear a pending debounce timer
    clear: function (timerId) {
        if (this.timers[timerId]) {
            clearTimeout(this.timers[timerId]);
            delete this.timers[timerId];
        }
    }
};

// Listen for system theme changes
window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
    // Only auto-switch if user hasn't set a preference
    if (!localStorage.getItem('theme')) {
        window.themeManager.applyTheme(e.matches);
    }
});
