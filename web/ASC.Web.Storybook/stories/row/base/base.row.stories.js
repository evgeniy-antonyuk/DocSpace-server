import React from 'react';
import { storiesOf } from '@storybook/react';
import { withKnobs, boolean, text, select } from '@storybook/addon-knobs/react';
import withReadme from 'storybook-readme/with-readme';
import Readme from './README.md';
import { Row, Text, Avatar, ComboBox, Icons } from 'asc-web-components';
import Section from '../../../.storybook/decorators/section';

storiesOf('Components|Row', module)
  .addDecorator(withKnobs)
  .addDecorator(withReadme(Readme))
  .add('base', () => {

    const contextButton = boolean('contextButton', true);
    const checked = boolean('checkbox', true);
    const element = select('element',['', 'Avatar', 'Icon', 'ComboBox'], '');

    const elementAvatar = <Avatar size='small' role='user' userName='Demo Avatar' />;
    const elementIcon = <Icons.CatalogFolderIcon size="big" />;
    const elementComboBox = <ComboBox
                              options={
                                [
                                  {key: 1, icon: 'ItemActiveIcon', label: 'Open'}, 
                                  {key: 2, icon: 'CheckIcon', label: 'Closed'}
                                ]
                              }
                              onSelect={option => console.log(option)}
                              selectedOption={{
                                key: 0,
                                icon: 'ItemActiveIcon',
                                label: ''
                              }}
                              scaled={false}
                              size='content'
                              isDisabled={false}
                            />;

    const checkedProps = checked ? { checked: false } : {};
    const getElementProps = element => 
      (element === 'Avatar') 
        ? { element: elementAvatar } 
        : (element === 'Icon') 
          ? { element: elementIcon } 
          : (element === 'ComboBox') 
            ? { element: elementComboBox } 
            : {};

    const elementProps = getElementProps(element);
  
    return (
      <Section>
        <Row 
          key='1'
          {...checkedProps}
          {...elementProps}
          contextOptions={contextButton ? [
            { key: 'key1', label: 'Edit', onClick: () => console.log('Context action: Edit') },
            { key: 'key2', label: 'Delete', onClick: () => console.log('Context action: Delete') }
          ] : []}
        >
          <Text.Body truncate={true} >{text('content', 'Sample text')}</Text.Body>
        </Row>
      </Section>
    );
  });
