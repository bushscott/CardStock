// Cardstock — shared nav search (grouped typeahead over the demo corpus).
// Load in <helmet> via <script src="./cardstock-search.js"></script>, then place <cardstock-search></cardstock-search> in the nav.
// Shadow DOM: internals are invisible to the page's renderer, so streaming re-renders can't duplicate the input.
(function () {
  if (customElements.get('cardstock-search')) return;
  var SPECIES = ['Charizard', 'Umbreon', 'Lugia', 'Rayquaza', 'Mewtwo', 'Espeon', 'Giratina', 'Blastoise', 'Gengar', 'Sylveon', 'Snorlax', 'Alakazam', 'Machamp', 'Dragonite', 'Leafeon', 'Glaceon'];
  var SETS = ['Base Set', 'Neo Genesis', 'Hidden Fates', 'Sword & Shield', 'Evolving Skies', 'Fusion Strike', 'Brilliant Stars', 'Lost Origin', 'Silver Tempest', 'Vivid Voltage'];
  var CARDS = [
    { n: 'Umbreon VMAX (Alt Art)', s: 'Evolving Skies' },
    { n: 'Espeon VMAX (Alt Art)', s: 'Evolving Skies' },
    { n: 'Sylveon VMAX (Alt Art)', s: 'Evolving Skies' },
    { n: 'Rayquaza VMAX (Alt Art)', s: 'Evolving Skies' },
    { n: 'Giratina V (Alt Art)', s: 'Lost Origin' },
    { n: 'Lugia V (Alt Art)', s: 'Silver Tempest' },
    { n: 'Gengar VMAX (Alt Art)', s: 'Fusion Strike' },
    { n: 'Charizard V (Brilliant Stars)', s: 'Brilliant Stars' },
    { n: 'Charizard Holo (Base Set)', s: 'Base Set' },
    { n: 'Blastoise Holo (Base Set)', s: 'Base Set' },
    { n: 'Machamp (1st Edition)', s: 'Base Set' },
    { n: 'Alakazam Holo', s: 'Base Set' },
    { n: 'Mewtwo GX (Shiny)', s: 'Hidden Fates' },
    { n: 'Lugia Holo (Neo Genesis)', s: 'Neo Genesis' },
    { n: 'Dragonite V (Alt Art)', s: 'Evolving Skies' }
  ];
  var CSS = ":host{display:block;position:relative;width:280px;height:30px}" +
    ".cs-in{width:100%;box-sizing:border-box;height:30px;border:1px solid var(--line,#E4E4E0);border-radius:6px;background:var(--bg,#FAFAF7);padding:0 30px 0 10px;font-family:'Inter',system-ui,sans-serif;font-size:15px;color:var(--ink,#1C1C1E)}" +
    ".cs-kbd{position:absolute;right:8px;top:6px;font-family:'JetBrains Mono',monospace;font-size:12.5px;color:var(--mut2,#6B6B66);border:1px solid var(--line,#E4E4E0);border-radius:4px;padding:0 5px;line-height:16px;background:var(--card,#FFFFFF)}" +
    ".cs-menu{position:absolute;top:36px;left:0;right:-60px;z-index:80;background:var(--card,#FFFFFF);border:1px solid var(--line,#E4E4E0);border-radius:8px;box-shadow:0 10px 28px rgba(20,19,26,0.14);padding:5px;max-height:340px;overflow-y:auto}" +
    ".cs-grp{font-size:10.5px;font-weight:600;letter-spacing:0.07em;color:var(--mut2,#6B6B66);text-transform:uppercase;padding:6px 8px 2px 8px}" +
    ".cs-item{display:flex;align-items:baseline;gap:8px;border-radius:5px;padding:5px 8px;font-size:13.5px;color:var(--ink,#1C1C1E);text-decoration:none}" +
    ".cs-item:hover{background:var(--hov,#F6F6F2)}" +
    ".cs-name{font-weight:500}" +
    ".cs-sub{font-size:12px;color:var(--mut2,#6B6B66)}" +
    ".cs-none{padding:8px;font-size:12.5px;color:var(--mut2,#6B6B66)}";
  function groupsFor(q) {
    var chars = SPECIES.filter(function (n) { return n.toLowerCase().indexOf(q) !== -1; }).slice(0, 4)
      .map(function (n) { return { name: n, sub: 'character', href: 'Cardstock Character.dc.html' }; });
    var sets = SETS.filter(function (n) { return n.toLowerCase().indexOf(q) !== -1; }).slice(0, 4)
      .map(function (n) { return { name: n, sub: 'set', href: 'Cardstock Set.dc.html' }; });
    var cards = CARDS.filter(function (c) { return c.n.toLowerCase().indexOf(q) !== -1; }).slice(0, 5)
      .map(function (c) { return { name: c.n, sub: c.s, href: 'Cardstock Card.dc.html' }; });
    var g = [];
    if (chars.length) g.push({ label: 'Characters', items: chars });
    if (sets.length) g.push({ label: 'Sets', items: sets });
    if (cards.length) g.push({ label: 'Cards', items: cards });
    return g;
  }
  class CardstockSearch extends HTMLElement {
    connectedCallback() {
      if (!this.shadowRoot) {
        var root = this.attachShadow({ mode: 'open' });
        var st = document.createElement('style');
        st.textContent = CSS;
        this._in = document.createElement('input');
        this._in.type = 'text';
        this._in.className = 'cs-in';
        this._in.placeholder = 'Search cards, sets, characters';
        this._in.setAttribute('aria-label', 'Search');
        this._in.autocomplete = 'off';
        var kbd = document.createElement('span');
        kbd.className = 'cs-kbd';
        kbd.textContent = '/';
        this._menu = document.createElement('div');
        this._menu.className = 'cs-menu';
        this._menu.style.display = 'none';
        root.appendChild(st);
        root.appendChild(this._in);
        root.appendChild(kbd);
        root.appendChild(this._menu);
        var self = this;
        this._in.addEventListener('input', function () { self._render(); });
        this._onDoc = function (e) { if (!self.contains(e.target)) self._clear(); };
        this._onKey = function (e) {
          var inSelf = document.activeElement === self;
          var tag = (document.activeElement && document.activeElement.tagName) || '';
          if (e.key === '/' && !inSelf && !/input|select|textarea/i.test(tag)) { e.preventDefault(); self._in.focus(); }
          else if (e.key === 'Escape' && (self._in.value || inSelf)) { self._clear(); self._in.blur(); }
        };
      }
      document.addEventListener('mousedown', this._onDoc);
      document.addEventListener('keydown', this._onKey);
    }
    disconnectedCallback() {
      document.removeEventListener('mousedown', this._onDoc);
      document.removeEventListener('keydown', this._onKey);
    }
    _clear() {
      if (!this._in) return;
      this._in.value = '';
      this._render();
    }
    _render() {
      var q = this._in.value.trim().toLowerCase();
      var menu = this._menu;
      menu.textContent = '';
      if (q.length < 2) { menu.style.display = 'none'; return; }
      var groups = groupsFor(q);
      menu.style.display = 'block';
      if (!groups.length) {
        var none = document.createElement('div');
        none.className = 'cs-none';
        none.textContent = 'No matches for \u201C' + this._in.value.trim() + '\u201D';
        menu.appendChild(none);
        return;
      }
      groups.forEach(function (g) {
        var h = document.createElement('div');
        h.className = 'cs-grp';
        h.textContent = g.label;
        menu.appendChild(h);
        g.items.forEach(function (it) {
          var a = document.createElement('a');
          a.className = 'cs-item';
          a.href = it.href;
          var nm = document.createElement('span');
          nm.className = 'cs-name';
          nm.textContent = it.name;
          var sb = document.createElement('span');
          sb.className = 'cs-sub';
          sb.textContent = it.sub;
          a.appendChild(nm);
          a.appendChild(sb);
          menu.appendChild(a);
        });
      });
    }
  }
  customElements.define('cardstock-search', CardstockSearch);
})();
