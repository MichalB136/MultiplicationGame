// Drag & drop for multiplication table (input to input)
let dragSourceInput = null;

// Helper function to show user-facing validation warning
function showInputWarning(input, message) {
	// Remove previous warning if exists
	const existingWarning = input.parentElement.querySelector('.input-warning');
	if (existingWarning) {
		existingWarning.remove();
	}
	
	// Add red border to input
	input.style.borderColor = '#dc3545';
	input.style.borderWidth = '2px';
	input.style.boxShadow = '0 0 5px rgba(220, 53, 69, 0.5)';
	
	// Create and show warning message
	const warningDiv = document.createElement('div');
	warningDiv.className = 'input-warning alert alert-danger mt-1 py-1 px-2 small';
	warningDiv.textContent = message;
	warningDiv.style.fontSize = '0.85rem';
	warningDiv.style.marginTop = '0.25rem';
	input.parentElement.appendChild(warningDiv);
	
	// Auto-remove warning after 3 seconds
	setTimeout(() => {
		warningDiv.remove();
		// Reset border style
		input.style.borderColor = '';
		input.style.borderWidth = '';
		input.style.boxShadow = '';
	}, 3000);
	
	// Log to console
	console.warn(`⚠️ User warning: ${message}`);
}

window.dragInputValue = function(ev) {
	ev.dataTransfer.setData("text", ev.target.value);
	dragSourceInput = ev.target;
}
window.allowDrop = function(ev) {
	ev.preventDefault();
}
window.dragAnswer = function(ev) {
	ev.dataTransfer.setData("text", ev.target.getAttribute("data-value"));
	dragSourceInput = null;
}
window.dropAnswer = function(ev) {
	ev.preventDefault();
	var value = ev.dataTransfer.getData("text");
	if (!/^[0-9]+$/.test(value)) return;
	ev.target.value = value;
	if (dragSourceInput && dragSourceInput !== ev.target) {
		dragSourceInput.value = "";
	}
	dragSourceInput = null;
}

// ============================================================================
// INPUT VALIDATION - Event delegation pattern (works for dynamic elements)
// ============================================================================

document.addEventListener('keydown', function(e) {
	// Only process numeric input fields
	if (!e.target.matches('input.numeric-input')) return;
	
	const input = e.target;
	const key = e.key;
	// Prefer e.key (works reliably); e.which is deprecated and can be 0/undefined
	const char = (typeof key === 'string' && key.length === 1) ? key : '';
	const isMultiplicationInput = input.name === 'UserAnswerText';
	
	console.log(`[keydown] key="${key}", char="${char}", field="${input.name}"`);
	
	// ALLOW: Navigation and edit keys
	if (['ArrowLeft', 'ArrowRight', 'Home', 'End', 'Backspace', 'Delete', 'Tab', 'Enter'].includes(key)) {
		return;
	}
	
	// BLOCK: Space character - CRITICAL
	if (key === ' ' || e.code === 'Space' || char === ' ') {
		console.error(`🔴 [Input Validation] BLOCKED SPACE in "${input.name}"`);
		showInputWarning(input, 'Spacje nie są dozwolone. Wpisz tylko cyfrę lub liczbę.');
		e.preventDefault();
		return;
	}
	
	// ALLOW: Ctrl+C, Ctrl+A, Ctrl+V, Ctrl+X (standard edit shortcuts)
	if (e.ctrlKey || e.metaKey) {
		if (['c', 'v', 'x', 'a', 'C', 'V', 'X', 'A', 'z', 'Z'].includes(key)) {
			return;
		}
		// Block other Ctrl combinations
		console.warn(`⚠️ [Input Validation] Blocked Ctrl+${key}`);
		e.preventDefault();
		return;
	}
	
	// ALLOW: Digits
	if (/^[0-9]$/.test(key)) {
		return;
	}
	
	// ALLOW: Minus only at position 0
	if (key === '-' && input.selectionStart === 0 && !input.value.includes('-')) {
		return;
	}
	
	// FOR MULTIPLICATION: Block decimals completely
	if (isMultiplicationInput) {
		if (key === '.' || key === ',') {
			console.error(`🔴 [Multiplication] Blocked decimal "${char}" - integers only`);
			showInputWarning(input, 'Gra mnożenia przyjmuje tylko liczby całkowite (bez przecinków ani kropek).');
			e.preventDefault();
			return;
		}
	} else {
		// FOR EQUATIONS: Allow one decimal separator (either . or ,)
		if ((key === '.' || key === ',') && 
		    !input.value.includes('.') && !input.value.includes(',')) {
			console.log(`✓ [Equations] Allowing decimal separator "${char}"`);
			return;
		}
		if (key === '.' || key === ',') {
			console.warn(`⚠️ [Equations] Already has decimal separator, blocking "${char}"`);
			e.preventDefault();
			return;
		}
	}
	
	// BLOCK: Any other character
	console.error(`🔴 [Input Validation] Rejected "${char}" (key="${key}") in field "${input.name}"`);
	showInputWarning(input, `Znak '${char}' nie jest dozwolony. Wpisz tylko cyfry.`);
	e.preventDefault();
}, false); // Use bubbling phase (false = default)

// Safety net: Clean input on each keystroke
document.addEventListener('input', function(e) {
	if (!e.target.matches('input.numeric-input')) return;
	
	const input = e.target;
	const originalValue = input.value;
	const isMultiplicationInput = input.name === 'UserAnswerText';
	
	let value = originalValue;
	
	// CRITICAL: Remove ALL whitespace - spaces are the main attack vector
	const spaceCount = (value.match(/\s/g) || []).length;
	value = value.replace(/\s+/g, '');
	
	// Remove invisible Unicode
	value = value.replace(/[\u200B\u200C\u200D\u202A\u202B\u202C\u202D\u202E\u2060\uFEFF\u061C]/g, '');
	
	if (isMultiplicationInput) {
		// Multiplication: only digits and minus
		value = value.replace(/[^0-9\-]/g, '');
		
		// Ensure minus only at start
		const hasMinus = value.startsWith('-');
		value = value.replace(/-/g, '');
		if (hasMinus && value) {
			value = '-' + value;
		}
	} else {
		// Equations: allow decimal point
		const hasMinus = value.startsWith('-');
		value = value.replace(/-/g, '');
		
		// Normalize comma to dot
		const parts = value.split(/[.,]/);
		value = parts[0] + (parts.length > 1 ? '.' + parts.slice(1).join('') : '');
		
		// Keep only one decimal point
		const dotIndex = value.indexOf('.');
		if (dotIndex !== -1) {
			const [intPart, decPart] = value.split('.');
			value = intPart + '.' + decPart.replace(/[^0-9]/g, '');
		}
		
		if (hasMinus && value && value !== '-') {
			value = '-' + value;
		}
	}
	
	// Update input if value changed
	if (input.value !== value) {
		console.error(`🔴 [${isMultiplicationInput ? 'Multiplication' : 'Equations'}] CORRECTION: "${originalValue}" → "${value}"`);
		if (spaceCount > 0) {
			showInputWarning(input, `Usunąłem ${spaceCount} ${spaceCount === 1 ? 'spację' : 'spacje'}. Zostało: ${value}`);
		}
		input.value = value;
	}
}, false);

// Block paste in multiplication game
document.addEventListener('paste', function(e) {
	if (!e.target.matches('input.numeric-input')) return;
	
	const input = e.target;
	const isMultiplicationInput = input.name === 'UserAnswerText';
	
	if (isMultiplicationInput) {
		console.warn(`⚠️ [Multiplication] Paste blocked`);
		e.preventDefault();
		showInputWarning(input, 'Wklejanie jest wyłączone. Wpisz liczbę ręcznie.');
		return;
	}
	
	// For equations: clean pasted content
	e.preventDefault();
	const pasted = (e.clipboardData || window.clipboardData).getData('text');
	console.log(`📋 [Equations] Pasted: "${pasted}"`);
	
	let cleaned = pasted
		.replace(/[\s\u200B\u200C\u200D\u202A\u202B\u202C\u202D\u202E\u2060\uFEFF\u061C]/g, '')
		.replace(/[^0-9,.\-]/g, '');
	
	const hasMinus = cleaned.startsWith('-');
	cleaned = cleaned.replace(/-/g, '');
	
	const parts = cleaned.split(/[.,]/);
	cleaned = parts[0] + (parts.length > 1 ? '.' + parts.slice(1).join('').replace(/[^0-9]/g, '') : '');
	
	if (hasMinus && cleaned) {
		cleaned = '-' + cleaned;
	}
	
	console.log(`📋 [Equations] Cleaned: "${pasted}" → "${cleaned}"`);
	input.value = cleaned;
}, false);

// AJAX form submission for game forms to avoid full page reload
document.addEventListener('submit', async function(e) {
	const form = e.target;
	if (!form) return;

	// Only handle forms with class "question-form" (game forms)
	if (!form.classList.contains('question-form')) return;

	e.preventDefault(); // Always prevent default for game forms

	// Multiplication form validation
	const multInput = form.querySelector('input[name="UserAnswerText"].numeric-input');
	if (multInput) {
		const value = (multInput.value || '').trim();
		if (!/^-?\d+$/.test(value)) {
			showInputWarning(multInput, 'Dozwolone są tylko cyfry (opcjonalnie minus na początku).');
			console.error(`🔴 [Submit Guard] Blocked submit (multiplication) invalid value: "${value}"`);
			return;
		}
	}

	// Equations form validation
	const eqInput = form.querySelector('input[name="UserAnswer"].numeric-input');
	if (eqInput) {
		const value = (eqInput.value || '').trim();
		if (!/^-?(?:\d+(?:[.,]\d+)?|[.,]\d+)$/.test(value)) {
			showInputWarning(eqInput, 'Wpisz liczbę (może być ułamek dziesiętny, dopuszczalne . lub ,).');
			console.error(`🔴 [Submit Guard] Blocked submit (equations) invalid value: "${value}"`);
			return;
		}
	}

	// Submit via AJAX
	try {
		console.log('📤 Submitting form via AJAX...');
		const formData = new FormData(form);
		
		// Determine target URL based on form action or current page
		const url = form.action || window.location.pathname;
		
		const response = await fetch(url, {
			method: 'POST',
			body: formData,
			headers: {
				'X-Requested-With': 'XMLHttpRequest' // Signal AJAX request
			}
		});

		if (!response.ok) {
			console.error('❌ Server error:', response.status);
			return;
		}

		const html = await response.text();
		console.log('✅ Received response, updating fragment...');

		// Update the interactive fragment without reloading page
		const fragmentContainer = document.getElementById('interactiveFragment');
		if (fragmentContainer) {
			fragmentContainer.innerHTML = html;
			console.log('🎯 Fragment updated successfully - NO PAGE RELOAD');
		} else {
			console.warn('⚠️ Fragment container not found, fallback to full page load');
			document.body.innerHTML = html;
		}

	} catch (error) {
		console.error('❌ AJAX submission failed:', error);
		// Fallback to traditional submit if AJAX fails
		form.submit();
	}
}, true);
