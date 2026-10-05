// 卡册：加载全部卡面贴图，点缩略图就换卡（也支持 #2 这种地址直达）。
(function () {
  var CARDS = window.__HOLO_CARDS__ || [];
  var SLOTS = ["subject", "background", "lineart", "text"];

  function loadImage(src) {
    return new Promise(function (res, rej) {
      var img = new Image();
      img.onload = function () { res(img); };
      img.onerror = rej;
      img.src = src;
    });
  }

  function preload(card) {
    return Promise.all(SLOTS.map(function (k) { return loadImage(card.assets[k]); }));
  }

  function setText(id, value) {
    var el = document.getElementById(id);
    if (el) el.textContent = value;
  }

  function buildRail(images, current) {
    var rail = document.getElementById("deck-rail");
    if (!rail) return;
    rail.textContent = "";
    CARDS.forEach(function (card, i) {
      var btn = document.createElement("button");
      btn.type = "button";
      btn.className = "deck-card" + (i === current ? " on" : "");
      btn.setAttribute("aria-pressed", String(i === current));
      var thumb = document.createElement("span");
      thumb.className = "thumb";
      ["background", "subject", "text"].forEach(function (k) {
        var im = document.createElement("img");
        im.src = card.assets[k];
        im.alt = "";
        thumb.appendChild(im);
      });
      var meta = document.createElement("span");
      meta.className = "deck-meta";
      var b = document.createElement("b");
      b.textContent = card.title;
      var s = document.createElement("i");
      s.textContent = card.subtitle;
      var n = document.createElement("u");
      n.textContent = "NO." + card.no + " · " + card.rarity;
      meta.appendChild(b);
      meta.appendChild(s);
      meta.appendChild(n);
      btn.appendChild(thumb);
      btn.appendChild(meta);
      btn.addEventListener("click", function () { select(i, images); });
      rail.appendChild(btn);
    });
  }

  function select(index, images) {
    var holo = window.__holo;
    if (!holo || !holo.ready) return;
    var card = CARDS[index];
    if (!card) return;
    var u = holo.uniforms;
    var imgs = images[index];

    ["tSubject", "tBackground", "tLine", "tText"].forEach(function (slot, k) {
      var old = u[slot].value;
      var tex = new old.constructor(imgs[k]);
      tex.colorSpace = old.colorSpace;
      tex.anisotropy = old.anisotropy;
      tex.needsUpdate = true;
      u[slot].value = tex;
    });
    if (typeof card.foil === "number") u.uFoil.value = card.foil;

    setText("card-title", card.title);
    setText("subtitle", card.subtitle);
    setText("description", card.description);
    setText("edition", "NO." + card.no + " / " + card.total);
    setText("about-title", card.title + " / " + card.subtitle);
    setText("about-description", card.description);
    setText("about-edition", "NO." + card.no + " / " + card.total);
    setText("deck-count", (index + 1) + " / " + CARDS.length);
    document.title = card.title + " · " + card.subtitle + " · 全息典藏卡册";

    var foil = document.getElementById("foil");
    if (foil) {
      foil.value = String(card.foil);
      foil.dispatchEvent(new Event("input", { bubbles: true }));
    }
    var swatch = document.querySelector('[data-finish="' + card.finish + '"]');
    if (swatch) swatch.click();

    var rail = document.getElementById("deck-rail");
    if (rail) {
      Array.prototype.forEach.call(rail.children, function (el, i) {
        el.classList.toggle("on", i === index);
        el.setAttribute("aria-pressed", String(i === index));
      });
    }
    if (location.hash !== "#" + card.no) history.replaceState(null, "", "#" + card.no);
  }

  function boot() {
    if (!CARDS.length) return;
    Promise.all(CARDS.map(preload)).then(function (images) {
      var start = 0;
      var want = (location.hash || "").replace("#", "");
      CARDS.forEach(function (c, i) { if (c.no === want || String(i + 1) === want) start = i; });
      buildRail(images, start);
      var tries = 0;
      var timer = setInterval(function () {
        tries += 1;
        if (window.__holo && window.__holo.ready) {
          clearInterval(timer);
          select(start, images);
        } else if (tries > 200) {
          clearInterval(timer);
        }
      }, 100);
    });
  }

  if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", boot);
  else boot();
})();
