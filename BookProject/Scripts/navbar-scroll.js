// קוד קיים - אפקט הסקרול
window.addEventListener('scroll', function() {
    const nav = document.querySelector('nav');
    if (window.scrollY > 50) {
        nav.classList.add('scrolled');
    } else {
        nav.classList.remove('scrolled');
    }
});

// קוד מעודכן - אפקט הקו הנע
document.addEventListener('DOMContentLoaded', function() {
    const nav = document.querySelector('nav ul');
    const movingLine = document.createElement('div');
    movingLine.className = 'moving-line';
    nav.appendChild(movingLine);

    // מוסיף קלאס active לדף הנוכחי
    const currentPath = window.location.pathname;
    nav.querySelectorAll('li a').forEach(link => {
        if (link.getAttribute('href') === currentPath) {
            link.classList.add('active');
        }
    });

    // מיקום ראשוני על הדף הפעיל
    const activeLink = nav.querySelector('a.active');
    if (activeLink) {
        positionLine(activeLink);
    }

    // הזזת הקו בהובר
    nav.querySelectorAll('li a').forEach(link => {
        link.addEventListener('mouseenter', function() {
            positionLine(this);
        });
    });

    // החזרת הקו לדף הפעיל כשהעכבר יוצא
    nav.addEventListener('mouseleave', function() {
        const activeLink = nav.querySelector('a.active');
        if (activeLink) {
            positionLine(activeLink);
        }
    });

    function positionLine(link) {
        movingLine.style.width = `${link.offsetWidth}px`;
        movingLine.style.left = `${link.offsetLeft}px`;
    }
});