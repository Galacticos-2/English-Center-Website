import { useEffect, useState } from "react";
import { BookOpen, FileUp, Pencil, Plus, Trash2, X } from "lucide-react";
import { getMyTeacherClassesApi } from "../../lesson/api/lessonApi";
import {
    createFlashcardApi,
    deleteFlashcardApi,
    deleteFlashcardsApi,
    getTeacherFlashcardsApi,
    importFlashcardsApi,
    previewFlashcardImportApi,
    updateFlashcardApi
} from "../api/flashcardApi";
import "../styles/flashcard.css";

const emptyForm = { classRoomId: "", word: "", meaning: "", exampleSentence: "" };

function TeacherFlashcardContent() {
    const [flashcards, setFlashcards] = useState([]);
    const [classes, setClasses] = useState([]);
    const [form, setForm] = useState(emptyForm);
    const [editingId, setEditingId] = useState(null);
    const [showForm, setShowForm] = useState(false);
    const [showImport, setShowImport] = useState(false);
    const [importClassRoomId, setImportClassRoomId] = useState("");
    const [importFile, setImportFile] = useState(null);
    const [importPreview, setImportPreview] = useState(null);
    const [loading, setLoading] = useState(true);
    const [submitting, setSubmitting] = useState(false);
    const [importing, setImporting] = useState(false);
    const [selectedFlashcardIds, setSelectedFlashcardIds] = useState(new Set());
    const [deletingSelected, setDeletingSelected] = useState(false);
    const [error, setError] = useState("");
    const [success, setSuccess] = useState("");

    const load = async () => {
        setLoading(true);
        setError("");
        try {
            const [flashcardData, classData] = await Promise.all([getTeacherFlashcardsApi(), getMyTeacherClassesApi()]);
            setFlashcards(flashcardData || []);
            setClasses(classData || []);
            setSelectedFlashcardIds(new Set());
        } catch (requestError) {
            setError(requestError.response?.data?.message || "Unable to load flashcards.");
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => { load(); }, []);

    const closeForm = () => {
        setForm(emptyForm);
        setEditingId(null);
        setShowForm(false);
    };

    const openCreate = () => {
        setError("");
        setSuccess("");
        setForm(emptyForm);
        setEditingId(null);
        setShowForm(true);
    };

    const edit = flashcard => {
        setError("");
        setSuccess("");
        setEditingId(flashcard.vocabularyFlashcardId);
        setForm({
            classRoomId: String(flashcard.classRoomId),
            word: flashcard.word,
            meaning: flashcard.meaning,
            exampleSentence: flashcard.exampleSentence
        });
        setShowForm(true);
    };

    const closeImport = () => {
        setShowImport(false);
        setImportClassRoomId("");
        setImportFile(null);
        setImportPreview(null);
        setImporting(false);
    };

    const openImport = () => {
        setError("");
        setSuccess("");
        setShowImport(true);
    };

    const selectImportFile = event => {
        setImportFile(event.target.files?.[0] || null);
        setImportPreview(null);
    };

    const previewImport = async () => {
        if (!importClassRoomId || !importFile) {
            setError("Select a class and an Excel file before checking the import.");
            return;
        }

        setImporting(true);
        setError("");
        try {
            const response = await previewFlashcardImportApi(Number(importClassRoomId), importFile);
            setImportPreview(response.data.data);
        } catch (requestError) {
            setError(requestError.response?.data?.message || "Unable to check the flashcard file.");
        } finally {
            setImporting(false);
        }
    };

    const confirmImport = async () => {
        if (!importFile || !importPreview?.canImport) return;

        setImporting(true);
        setError("");
        try {
            const result = await importFlashcardsApi(Number(importClassRoomId), importFile);
            closeImport();
            setSuccess(`Imported ${result.createdCount} flashcards. Duplicate words were skipped.`);
            await load();
        } catch (requestError) {
            setError(requestError.response?.data?.message || "Unable to import flashcards.");
        } finally {
            setImporting(false);
        }
    };

    const submit = async event => {
        event.preventDefault();
        setSubmitting(true);
        setError("");
        setSuccess("");
        try {
            const data = { ...form, classRoomId: Number(form.classRoomId) };
            if (editingId) await updateFlashcardApi(editingId, data);
            else await createFlashcardApi(data);
            closeForm();
            await load();
        } catch (requestError) {
            setError(requestError.response?.data?.message || "Unable to save flashcard.");
        } finally {
            setSubmitting(false);
        }
    };

    const remove = async flashcardId => {
        if (!window.confirm("Delete this flashcard?")) return;
        try {
            await deleteFlashcardApi(flashcardId);
            await load();
        } catch (requestError) {
            setError(requestError.response?.data?.message || "Unable to delete flashcard.");
        }
    };

    const toggleFlashcardSelection = flashcardId => {
        setSelectedFlashcardIds(current => {
            const next = new Set(current);
            if (next.has(flashcardId)) next.delete(flashcardId);
            else next.add(flashcardId);
            return next;
        });
    };

    const toggleSelectAll = () => {
        setSelectedFlashcardIds(current =>
            current.size === flashcards.length
                ? new Set()
                : new Set(flashcards.map(card => card.vocabularyFlashcardId))
        );
    };

    const removeSelected = async () => {
        const flashcardIds = [...selectedFlashcardIds];
        if (flashcardIds.length === 0) return;

        if (!window.confirm(`Delete ${flashcardIds.length} selected flashcard${flashcardIds.length === 1 ? "" : "s"}?`)) {
            return;
        }

        setDeletingSelected(true);
        setError("");
        setSuccess("");
        try {
            const result = await deleteFlashcardsApi(flashcardIds);
            setSuccess(`Deleted ${result.deletedCount} flashcard${result.deletedCount === 1 ? "" : "s"}.`);
            await load();
        } catch (requestError) {
            setError(requestError.response?.data?.message || "Unable to delete selected flashcards.");
        } finally {
            setDeletingSelected(false);
        }
    };

    const skippedCount = importPreview?.skippedRows?.length || 0;

    return (
        <section className="flashcard-page teacher-flashcard-page">
            <div className="flashcard-breadcrumb"><span>Home</span><span>/</span><strong>Flashcards</strong></div>
            <header className="flashcard-heading">
                <div className="flashcard-title"><div className="flashcard-title-icon"><BookOpen size={21} /></div><div><span>VOCABULARY PRACTICE</span><h1>Flashcards</h1><p>Create vocabulary cards for each of your classes.</p></div></div>
                <div className="flashcard-heading-actions"><button type="button" className="flashcard-secondary" onClick={openImport}><FileUp size={18} /> Import Excel</button><button type="button" className="flashcard-bulk-delete" disabled={selectedFlashcardIds.size === 0 || deletingSelected} onClick={removeSelected}><Trash2 size={18} /> {deletingSelected ? "Deleting..." : `Delete selected (${selectedFlashcardIds.size})`}</button><button type="button" className="flashcard-primary" onClick={openCreate}><Plus size={18} /> Add flashcard</button></div>
            </header>
            {error && <div className="flashcard-error">{error}</div>}
            {success && <div className="flashcard-success">{success}</div>}
            <div className="flashcard-list-panel">
                {loading ? <div className="flashcard-empty">Loading flashcards...</div> : flashcards.length === 0 ? <div className="flashcard-empty"><BookOpen size={36} /><h2>No flashcards yet</h2><p>Add vocabulary for a class so students can start learning.</p></div> : <div className="flashcard-table-wrap"><table className="flashcard-table"><thead><tr><th className="flashcard-select-column"><input type="checkbox" checked={selectedFlashcardIds.size === flashcards.length} onChange={toggleSelectAll} aria-label="Select all flashcards" /></th><th>Word</th><th>Meaning</th><th>Example sentence</th><th>Class</th><th aria-label="Actions" /></tr></thead><tbody>{flashcards.map(card => <tr key={card.vocabularyFlashcardId} className={selectedFlashcardIds.has(card.vocabularyFlashcardId) ? "is-selected" : ""}><td className="flashcard-select-column"><input type="checkbox" checked={selectedFlashcardIds.has(card.vocabularyFlashcardId)} onChange={() => toggleFlashcardSelection(card.vocabularyFlashcardId)} aria-label={`Select ${card.word}`} /></td><td><strong>{card.word}</strong></td><td>{card.meaning}</td><td>{card.exampleSentence}</td><td><span className="flashcard-class-tag">{card.classRoomName}</span></td><td className="flashcard-actions"><button type="button" onClick={() => edit(card)} aria-label="Edit flashcard"><Pencil size={16} /></button><button type="button" className="delete" onClick={() => remove(card.vocabularyFlashcardId)} aria-label="Delete flashcard"><Trash2 size={16} /></button></td></tr>)}</tbody></table></div>}
            </div>

            {showForm && <div className="flashcard-modal-backdrop"><form className="flashcard-modal" onSubmit={submit}><button type="button" className="flashcard-close" onClick={closeForm} aria-label="Close"><X size={20} /></button><h2>{editingId ? "Edit flashcard" : "Add a flashcard"}</h2><label>Class<select required value={form.classRoomId} onChange={event => setForm({ ...form, classRoomId: event.target.value })}><option value="">Select a class</option>{classes.map(item => <option key={item.classRoomId} value={item.classRoomId}>{item.className}{item.courseName ? ` — ${item.courseName}` : ""}</option>)}</select></label><label>Word<input required maxLength="150" value={form.word} onChange={event => setForm({ ...form, word: event.target.value })} placeholder="Example: achieve" /></label><label>Meaning<textarea required maxLength="500" value={form.meaning} onChange={event => setForm({ ...form, meaning: event.target.value })} placeholder="Explain the meaning in Vietnamese or English" /></label><label>Example sentence<textarea required maxLength="1000" value={form.exampleSentence} onChange={event => setForm({ ...form, exampleSentence: event.target.value })} placeholder="Example: She achieved her goal." /></label><button className="flashcard-primary" disabled={submitting} type="submit">{submitting ? "Saving..." : editingId ? "Save changes" : "Create flashcard"}</button></form></div>}

            {showImport && <div className="flashcard-modal-backdrop"><div className="flashcard-modal flashcard-import-modal" role="dialog" aria-modal="true" aria-labelledby="flashcard-import-title"><button type="button" className="flashcard-close" onClick={closeImport} aria-label="Close"><X size={20} /></button><h2 id="flashcard-import-title">Import flashcards from Excel</h2><p className="flashcard-import-note">Use an .xlsx file whose first row has: TỪ VỰNG, NGHĨA TIẾNG VIỆT, CÂU VÍ DỤ. Existing and repeated words are skipped automatically.</p>{error && <div className="flashcard-error">{error}</div>}<label>Class<select disabled={importing} value={importClassRoomId} onChange={event => { setImportClassRoomId(event.target.value); setImportPreview(null); }}><option value="">Select a class</option>{classes.map(item => <option key={item.classRoomId} value={item.classRoomId}>{item.className}{item.courseName ? ` — ${item.courseName}` : ""}</option>)}</select></label><label>Excel file<input disabled={importing} type="file" accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" onChange={selectImportFile} /></label>{importPreview && <div className={`flashcard-import-preview${importPreview.isValid ? " is-valid" : " has-errors"}`}><strong>{!importPreview.isValid ? `${importPreview.errors.length} issue${importPreview.errors.length === 1 ? "" : "s"} found. Nothing can be imported until every issue is fixed.` : importPreview.canImport ? `${importPreview.importableCount} flashcards are ready to import.` : "No new flashcards are available to import."}</strong>{skippedCount > 0 && <p className="flashcard-import-skipped">{skippedCount} duplicate {skippedCount === 1 ? "word was" : "words were"} skipped automatically.</p>}{importPreview.errors.length > 0 && <ul>{importPreview.errors.map((item, errorIndex) => <li key={`${item.rowNumber || "file"}-${errorIndex}`}>{item.rowNumber ? `Row ${item.rowNumber}: ` : ""}{item.message}</li>)}</ul>}{importPreview.previewRows.length > 0 && <div className="flashcard-import-sample"><span>Review all {importPreview.previewRows.length} flashcards to be imported</span><div className="flashcard-table-wrap"><table className="flashcard-table"><thead><tr><th>Row</th><th>Word</th><th>Meaning</th><th>Example sentence</th></tr></thead><tbody>{importPreview.previewRows.map(row => <tr key={row.rowNumber}><td>{row.rowNumber}</td><td>{row.word}</td><td>{row.meaning}</td><td>{row.exampleSentence}</td></tr>)}</tbody></table></div></div>}</div>}<div className="flashcard-import-actions"><button type="button" className="flashcard-secondary" disabled={importing} onClick={closeImport}>Cancel</button>{(!importPreview || !importPreview.isValid) && <button type="button" className="flashcard-primary" disabled={importing} onClick={previewImport}>{importing ? "Checking..." : "Check file"}</button>}{importPreview?.canImport && <button type="button" className="flashcard-primary" disabled={importing} onClick={confirmImport}>{importing ? "Importing..." : `Confirm import ${importPreview.importableCount} flashcards`}</button>}</div></div></div>}
        </section>
    );
}

export default TeacherFlashcardContent;
