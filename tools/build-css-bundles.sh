#!/bin/sh
set -eu

project_root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
css_root="$project_root/wwwroot/css"
bundle_root="$css_root/bundles"

mkdir -p "$bundle_root"

write_bundle() {
    output_name=$1
    template_style=$2
    output_path="$bundle_root/$output_name"
    temp_path="$output_path.tmp"

    : > "$temp_path"
    first_source=1
    for source_file in \
        site.css \
        contact.css \
        media.css \
        rich-content.css \
        mobile-navigation.css \
        content-variants.css \
        enhanced-sections.css \
        "$template_style"
    do
        source_path="$css_root/$source_file"
        if [ ! -f "$source_path" ]; then
            echo "CSS source not found: $source_path" >&2
            exit 1
        fi

        if [ "$first_source" -eq 0 ]; then
            printf '\n\n' >> "$temp_path"
        fi
        printf '/* Source: /css/%s */\n' "$source_file" >> "$temp_path"
        sed -e '$a\' "$source_path" >> "$temp_path"
        first_source=0
    done
    printf '\n' >> "$temp_path"

    mv "$temp_path" "$output_path"
}

write_bundle "corporate.bundle.css" "templates/corporate.css"
write_bundle "minimal.bundle.css" "templates/minimal.css"
write_bundle "editorial.bundle.css" "templates/editorial.css"
write_bundle "full-width.bundle.css" "templates/full-width.css"
write_bundle "conversion.bundle.css" "templates/conversion.css"
