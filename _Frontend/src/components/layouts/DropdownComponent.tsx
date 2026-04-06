import { useContext, useMemo } from 'react'
import SearchContextConsumer, { type GeoFeature } from '@/contexts/SearchContext';
import searchIcon from '@/assets/images/features/point-of-interest.svg'
import { useWorkspaceGeoSearch } from '@/hooks/useWorkspaceGeoSearch';

function aggregateEntityTotals(results: GeoFeature[]) {
    const map = new Map<string, { label: string; count: number }>();
    for (const item of results) {
        const id = item.sourceEntityId != null && item.sourceEntityId !== '' ? String(item.sourceEntityId) : '_';
        const label = item.sourceEntityLabel?.trim() || id;
        const cur = map.get(id) ?? { label, count: 0 };
        cur.count += 1;
        if (item.sourceEntityLabel?.trim()) cur.label = item.sourceEntityLabel.trim();
        map.set(id, cur);
    }
    return Array.from(map.entries()).map(([entityId, v]) => ({ entityId, ...v }));
}

export const DropdownComponent = () => {
    const { loadedGeoData, selectedGeo, setSelectedGeo, setPosition, initialPosition } = useContext(SearchContextConsumer);

    useWorkspaceGeoSearch();

    const entityTotals = useMemo(() => {
        const results = loadedGeoData?.results;
        if (!results?.length) return [];
        return aggregateEntityTotals(results);
    }, [loadedGeoData?.results]);

    const updatingSelectedSearchResult = (selected: GeoFeature) => {
        setSelectedGeo(selected)
        setPosition({ ...initialPosition, longitude: Number(selected.geometry.coordinates[0][0]), latitude: Number(selected.geometry.coordinates[0][1]) })
    };

    const baseClassListItem =
        "flex w-full cursor-pointer items-start gap-2 border-b border-b-slate-100 px-2 py-2 last:border-b-0 hover:bg-slate-50"
    const baseClassListItemSelected =
        "flex w-full cursor-pointer items-start gap-2 border-2 border-solid border-blue-500 bg-blue-50 px-2 py-2 last:border-b-0"
    
    const results =
        loadedGeoData !== null && loadedGeoData.results !== undefined && Array.isArray(loadedGeoData.results)
            ? loadedGeoData.results
            : null;

    const showEntitySummary = results !== null && results.length > 0 && entityTotals.length > 0;

    return (
        <div className="absolute left-0 top-full z-50 mt-2 flex max-h-[60vh] w-full flex-col overflow-hidden rounded-md border border-slate-200 bg-white shadow-xl">
            {showEntitySummary ? (
                <div className="shrink-0 rounded-t-md border-b border-slate-200 bg-white px-2.5 py-2 shadow-sm">
                    <p className="m-0 text-[11px] font-semibold uppercase tracking-wide text-slate-500">Results by entity</p>
                    <ul className="mt-1.5 flex flex-wrap gap-x-3 gap-y-1">
                        {entityTotals.map(({ entityId, label, count }) => (
                            <li key={entityId} className="text-xs text-slate-700">
                                <span className="font-medium text-slate-800">{label}</span>
                                <span className="ml-1 tabular-nums text-slate-500">({count})</span>
                            </li>
                        ))}
                    </ul>
                </div>
            ) : null}
            {results !== null && results.length > 0 ? (
                <div
                    className={`min-h-0 overflow-y-auto overscroll-contain ${
                        showEntitySummary ? 'max-h-[calc(60vh-5.75rem)]' : 'max-h-[60vh]'
                    }`}
                >
                    {results.map((item: GeoFeature) => (
                    <article
                        key={String(item.Id)}
                        className={
                            selectedGeo !== null && item.Id === selectedGeo.Id ? baseClassListItemSelected : baseClassListItem
                        }
                        onClick={() => updatingSelectedSearchResult(item)}
                    >
                        <img src={searchIcon} alt="" width="40" height="40" className="block shrink-0 rounded-md bg-slate-100 p-1" />
                        <div className="flex min-w-0 flex-1 flex-col justify-center">
                            <h5 className="m-0 text-sm font-semibold text-slate-800">{item.placeName}</h5>
                            {item.locality ? (
                                <p className="m-0 mt-0.5 text-xs text-slate-500">{item.locality}</p>
                            ) : null}
                            {item.sourceEntityLabel ? (
                                <p
                                    className="m-0 mt-1 truncate text-[11px] font-medium uppercase tracking-wide text-slate-400"
                                    title={item.sourceEntityLabel}
                                >
                                    {item.sourceEntityLabel}
                                </p>
                            ) : null}
                        </div>
                    </article>
                    ))}
                </div>
            ) : null}
        </div>
    )
}
