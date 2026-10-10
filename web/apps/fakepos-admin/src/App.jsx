import { useEffect, useState } from "react";
import { api, getApiBase, setApiBase } from "./api";

export default function App() {
  const [apiBase, setApiBaseState] = useState(getApiBase());
  const [view, setView] = useState({ screen: "locations" });
  const [locations, setLocations] = useState([]);
  const [tables, setTables] = useState([]);
  const [table, setTable] = useState(null);
  const [error, setError] = useState(null);

  useEffect(() => {
    loadLocations();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function loadLocations() {
    try {
      setError(null);
      setLocations(await api.locations());
      setView({ screen: "locations" });
    } catch (e) {
      setError(e.message);
    }
  }

  async function openLocation(loc) {
    try {
      setError(null);
      setTables(await api.tables(loc));
      setView({ screen: "tables", loc });
    } catch (e) {
      setError(e.message);
    }
  }

  async function openTable(loc, no) {
    try {
      setError(null);
      setTable(await api.table(loc, no));
      setView({ screen: "table", loc, no });
    } catch (e) {
      setError(e.message);
    }
  }

  async function addItem(loc, no, item) {
    try {
      setError(null);
      await api.addItem(loc, no, item);
      await openTable(loc, no);
    } catch (e) {
      setError(e.message);
    }
  }

  async function resetCheck(loc, no) {
    try {
      setError(null);
      await api.reset(loc, no);
      await openTable(loc, no);
    } catch (e) {
      setError(e.message);
    }
  }

  function saveApiBase(url) {
    setApiBase(url);
    setApiBaseState(url);
    loadLocations();
  }

  return (
    <div className="app">
      <header>
        <h1>Fake POS — термінал</h1>
        <ApiBaseField value={apiBase} onSave={saveApiBase} />
      </header>

      {error && <div className="error">{error}</div>}

      {view.screen === "locations" && <LocationsScreen locations={locations} onOpen={openLocation} />}

      {view.screen === "tables" && (
        <TablesScreen loc={view.loc} tables={tables} onBack={loadLocations} onOpen={(no) => openTable(view.loc, no)} />
      )}

      {view.screen === "table" && table && (
        <TableScreen
          loc={view.loc}
          no={view.no}
          table={table}
          onBack={() => openLocation(view.loc)}
          onAddItem={(item) => addItem(view.loc, view.no, item)}
          onReset={() => resetCheck(view.loc, view.no)}
        />
      )}
    </div>
  );
}

function ApiBaseField({ value, onSave }) {
  const [draft, setDraft] = useState(value);

  return (
    <div className="api-base">
      <label>
        API:
        <input value={draft} onChange={(e) => setDraft(e.target.value)} />
      </label>
      <button onClick={() => onSave(draft)}>Підключити</button>
    </div>
  );
}

function LocationsScreen({ locations, onOpen }) {
  return (
    <section>
      <h2>Локації</h2>
      <ul className="list">
        {locations.map((loc) => (
          <li key={loc}>
            <button className="link" onClick={() => onOpen(loc)}>
              {loc}
            </button>
          </li>
        ))}
      </ul>
    </section>
  );
}

function TablesScreen({ loc, tables, onBack, onOpen }) {
  return (
    <section>
      <button className="link" onClick={onBack}>
        &larr; локації
      </button>
      <h2>Заклад «{loc}» — столики</h2>
      <table>
        <thead>
          <tr>
            <th>Столик</th>
            <th>Стан</th>
            <th>Сума</th>
            <th>Rev</th>
          </tr>
        </thead>
        <tbody>
          {tables.map((t) => (
            <tr key={t.table_no}>
              <td>
                <button className="link" onClick={() => onOpen(t.table_no)}>
                  {t.table_no}
                </button>
              </td>
              <td>{t.check?.state ?? "—"}</td>
              <td>{t.check?.sum_total ?? "—"}</td>
              <td>{t.check?.rev ?? "—"}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}

function TableScreen({ loc, no, table, onBack, onAddItem, onReset }) {
  const [title, setTitle] = useState("");
  const [qty, setQty] = useState("1");
  const [price, setPrice] = useState("");
  const check = table.check;

  function submitItem(e) {
    e.preventDefault();
    if (!title || !price) {
      return;
    }

    onAddItem({ title, qty, price });
    setTitle("");
    setQty("1");
    setPrice("");
  }

  return (
    <section>
      <button className="link" onClick={onBack}>
        &larr; столики
      </button>
      <h2>
        «{loc}» · стіл {no}
      </h2>

      {!check && <p className="muted">Немає відкритого чека.</p>}

      {check && (
        <>
          <p>
            check_id: <b>{check.check_id}</b> · rev: <b>{check.rev}</b> · state: <b>{check.state}</b> · сума: <b>{check.sum_total}</b>
          </p>
          <table>
            <thead>
              <tr>
                <th>Line</th>
                <th>Позиція</th>
                <th>К-сть</th>
                <th>Ціна</th>
              </tr>
            </thead>
            <tbody>
              {check.items.map((item) => (
                <tr key={item.line}>
                  <td>{item.line}</td>
                  <td>
                    {item.title}
                    {item.mods.length > 0 && (
                      <span className="muted"> ({item.mods.map((m) => `${m.title} +${m.price}`).join(", ")})</span>
                    )}
                  </td>
                  <td>{item.qty}</td>
                  <td>{item.price}</td>
                </tr>
              ))}
              {check.disc.map((d) => (
                <tr key={d.label}>
                  <td colSpan={2}>{d.label}</td>
                  <td colSpan={2}>{d.value}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </>
      )}

      <form onSubmit={submitItem} className="add-item">
        <input placeholder="Назва" value={title} onChange={(e) => setTitle(e.target.value)} required />
        <input placeholder="К-сть" value={qty} onChange={(e) => setQty(e.target.value)} required />
        <input placeholder="Ціна" value={price} onChange={(e) => setPrice(e.target.value)} required />
        <button type="submit">Додати позицію</button>
      </form>

      <button className="danger" onClick={onReset}>
        Скинути чек
      </button>
    </section>
  );
}
