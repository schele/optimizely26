// Contact forms: the send button shows a spinner while the form is being sent, and forms rendered with a
// data-recaptcha-site-key get an invisible reCAPTCHA v3 token first.
// Google's script is only loaded once a visitor starts on the form, and the token is fetched as the form is sent,
// as tokens only last two minutes. Without a token (script blocked, offline) the form is sent anyway and the
// server tells the visitor, keeping what they typed.
(() => {
	let loading;

	function loadReCaptcha(siteKey) {
		loading ??= new Promise((resolve, reject) => {
			const script = document.createElement('script');
			script.src = `https://www.google.com/recaptcha/api.js?render=${encodeURIComponent(siteKey)}`;
			script.async = true;
			script.onload = () => grecaptcha.ready(resolve);
			script.onerror = reject;
			document.head.append(script);
		});

		return loading;
	}

	function setSending(button, sending) {
		const label = button.querySelector('[data-sending-text]');

		label.dataset.sendText ??= label.textContent;
		label.textContent = sending ? label.dataset.sendingText : label.dataset.sendText;
		button.querySelector('[data-send-icon]').hidden = sending;
		button.querySelector('.spinner-border').hidden = !sending;
		button.disabled = sending;
	}

	document.querySelectorAll('form.contact-form:not([data-contact-form-ready])').forEach(form => {
		form.dataset.contactFormReady = '';

		const siteKey = form.dataset.recaptchaSiteKey;
		// The name is ContactFormPost.ReCaptchaToken, as the view renders it with nameof
		const tokenInput = form.elements.namedItem('ReCaptchaToken');
		const button = form.querySelector('[type="submit"]');

		if (siteKey) {
			form.addEventListener('focusin', () => loadReCaptcha(siteKey).catch(() => { }), { once: true });
		}

		// Runs after the browser's own validation, so only forms that are about to be sent get here
		form.addEventListener('submit', async event => {
			event.preventDefault();
			setSending(button, true);

			if (siteKey) {
				try {
					await loadReCaptcha(siteKey);
					tokenInput.value = await grecaptcha.execute(siteKey, { action: 'contact' });
				} catch {
					tokenInput.value = '';
				}
			}

			// submit() doesn't fire the submit event again
			form.submit();
		});

		// Coming back with the back button restores the page as it was, still sending
		window.addEventListener('pageshow', () => setSending(button, false));
	});
})();
