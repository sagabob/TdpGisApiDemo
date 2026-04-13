import { useState, useCallback, useMemo, useEffect, type CSSProperties } from 'react';
import { debounce } from "lodash"

interface SimpleSearchComponentProps {
    searchValue: string;
    setSearchValue: (value: string) => void;
    debounceTimeout: number;
    cssClassName?: string;
    cssStyle?: CSSProperties | null;
}

const SimpleSearchComponent = ({ searchValue, setSearchValue, debounceTimeout, cssClassName, cssStyle }: SimpleSearchComponentProps) => {

    const [localsearchValue, setLocalSearchValue] = useState(searchValue)

    // Sync local state when external searchValue prop changes
    useEffect(() => {
        setLocalSearchValue(searchValue);
    }, [searchValue]);

    /***IMPORTANT useMemo is used here due to a warning when trying to use callback for the debounce function*/
    const delayInput = useMemo(
        () =>
            debounce((searchInput: string) => {
                setSearchValue(searchInput)
            }, debounceTimeout),
        [setSearchValue, debounceTimeout]
    );

    // Cleanup pending debounced calls on unmount
    useEffect(() => {
        return () => {
            delayInput.cancel();
        };
    }, [delayInput]);

    const handleSearchValueChange = useCallback(
        (event: string) => {
            setLocalSearchValue(event);
            delayInput(event);
        },
        [delayInput]
    );

    return (
        <>
            <input type="search" value={localsearchValue} onChange={e => handleSearchValueChange(e.target.value)}
                className={cssClassName} placeholder="Search..." aria-label="Search" style={cssStyle != null ? cssStyle : undefined} />
        </>
    )
}

export default SimpleSearchComponent